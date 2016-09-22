using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System;
using System.Linq;

namespace UnityEditor.AssetBundles
{
	public class AssetBundleBrowserWindow : EditorWindow
	{
		public static List<AssetBundleBrowserWindow> openWindows = new List<AssetBundleBrowserWindow>();
		class AssetBundleChangeListener : AssetPostprocessor
		{
			public void OnPostprocessAssetbundleNameChanged(string assetPath, string previousAssetBundleName, string newAssetBundleName)
			{
				foreach(var w in AssetBundleBrowserWindow.openWindows)
					w.ResetData(false);
			}
		}

		[MenuItem("Window/Asset Bundle Browser")]
		static void ShowWindow()
		{
			var window = GetWindow<AssetBundleBrowserWindow>();
			window.titleContent = new GUIContent("AssetBundles");
			window.Show();
			openWindows.Add(window);
		}

		[SerializeField]
		TreeViewState m_TreeViewState;
		AssetBundleBrowserTree m_TreeView;
		AssetBundleData assetBundleData;
		List<AssetBundleBrowserTree.TreeItem> selectedItems = new List<AssetBundleBrowserTree.TreeItem>();
		GUIStyle richTextStyle = new GUIStyle();

		void OnDestroy()
		{
			openWindows.Remove(this);
		}
		Vector2 scrollPosition = Vector2.zero;
		void OnGUI()
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Rebuild AssetBundle Data"))
				ResetData(true);

			if (m_TreeView == null)
			{
				if (assetBundleData == null)
				{
					assetBundleData = new AssetBundleData();
					new AssetBundleStructData().Create();
				}
				richTextStyle.richText = true;

				if (m_TreeViewState == null)
					m_TreeViewState = new TreeViewState();

				// Create tree view and reload
				m_TreeView = new AssetBundleBrowserTree(m_TreeViewState, assetBundleData);
				m_TreeView.Reload();
				m_TreeView.OnSelectionChanged += SelectionChanged;
			}

			GUILayout.EndHorizontal();
			m_TreeView.OnGUI(new Rect(0, 20, position.width / 2, position.height - 20));
			GUILayout.BeginArea(new Rect(position.width / 2, 20, position.width / 2, position.height - 20));
			List<AssetImporter> importers = null;
			if (selectedItems.Count > 0)
			{
				string[] bundles = AssetDatabase.GetAllAssetBundleNames();
				scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
				foreach (var i in selectedItems)
				{
					GUILayout.Space(10);
					AssetBundleData.AssetInfo asset = i.data.assetInfo;
					if (asset.type == AssetBundleData.AssetInfo.Type.Asset)
					{
						GUILayout.Label(asset.displayPath, richTextStyle);
						AssetImporter importer = AssetImporter.GetAtPath(asset.assetName);
						if (importers == null)
							importers = new List<AssetImporter>();
						importers.Add(importer);
						GUILayout.BeginVertical();
						//GUILayout.Label("<b><color=white>References</color></b>", richTextStyle);
						foreach (var r in asset.references)
							GUILayout.Label("\t" + asset.GetFullReferencePath(r), richTextStyle);
						GUILayout.EndVertical();
					}
					if (asset.type == AssetBundleData.AssetInfo.Type.Bundle)
					{
						GUILayout.Label(asset.displayName, richTextStyle);
						GUILayout.BeginVertical();
						foreach (var r in asset.rootDependencies)
							GUILayout.Label("<color=white>" + r + "</color>", richTextStyle);
						GUILayout.EndVertical();
					}
				}
				EditorGUILayout.EndScrollView();

				if (importers != null)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label("<b><color=white>AssetBundle</color></b>", richTextStyle);
					string bid = importers[0].assetBundleName;
					foreach (var i in importers)
					{
						if (i.assetBundleName != bid)
						{
							bid = "-";
							break;
						}
					}

					int currentBundleId = Array.IndexOf(bundles, bid);
					int bundleId = EditorGUILayout.Popup(currentBundleId, bundles);
					if (bundleId != currentBundleId)
					{
						if (EditorUtility.DisplayDialog("AssetBundle Change", "Move selected assets to " + bundles[bundleId] + "?", "Ok", "Cancel"))
						{
							foreach(var i in importers)
								i.assetBundleName = bundles[bundleId];
							ResetData(false);
						}
					}
					GUILayout.EndHorizontal();
				}

				if (GUILayout.Button("Select"))
				{
					//Selection.activeObjects = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.assetName);
				}

			}
			GUILayout.EndArea();
		}


		public void ResetData(bool rebuildData)
		{
			if (rebuildData)
				assetBundleData = null;
			if (assetBundleData != null)
				assetBundleData.Refresh();
			selectedItems.Clear();
			m_TreeViewState = null;
			m_TreeView = null;
			Repaint();
		}


		void SelectionChanged(IList<int> selectedIds)
		{
			selectedItems.Clear();
			IList<TreeViewItem> items = m_TreeView.GetRowsFromIDs(selectedIds);
			foreach (var i in items)
			{
			//	UnityEngine.GameObject o = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>((i as AssetBundleBrowserTree.AssetBundleBrowserTreeItem).data.assetInfo.assetName);
			//	if(o != null)
			//		EditorGUIUtility.PingObject(o);
				selectedItems.Add(i as AssetBundleBrowserTree.TreeItem);
			}
		}
	}
}