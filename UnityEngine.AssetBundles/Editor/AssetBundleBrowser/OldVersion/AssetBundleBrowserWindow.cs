using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

namespace UnityEngine.AssetBundles.Old
{
	public class AssetBundleBrowserWindow : EditorWindow
	{
		public static List<AssetBundleBrowserWindow> openWindows = new List<AssetBundleBrowserWindow>();
		class AssetBundleChangeListener : AssetPostprocessor
		{
			public void OnPostprocessAssetbundleNameChanged(string assetPath, string previousAssetBundleName, string newAssetBundleName)
			{
				foreach(var w in AssetBundleBrowserWindow.openWindows)
					w.ResetData();
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
		AssetBundleData assetBundleData = new AssetBundleData();
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
			if (GUILayout.Button("Refresh Dependencies"))
				ResetData();
		/*	if (GUILayout.Button("Build Bundles"))
				BuildPipeline.BuildAssetBundles("AssetBundles", BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
			if (GUILayout.Button("Reset All Bundles"))
			{
				foreach (var a in assetBundleData.assetInfoMap.Values)
				{
					AssetImporter i = AssetImporter.GetAtPath(a.assetName);
					if (i != null)
					{
						if (i.assetBundleName != string.Empty)
						{
							i.assetBundleVariant = string.Empty;
							i.assetBundleName = string.Empty;
						}
					}
				}
				ResetData();
			}
			*/
			GUILayout.EndHorizontal();

			if (m_TreeView == null)
			{
				assetBundleData.Refresh(false);

				richTextStyle.richText = true;

				if (m_TreeViewState == null)
					m_TreeViewState = new TreeViewState();

				// Create tree view and reload
				m_TreeView = new AssetBundleBrowserTree(m_TreeViewState, assetBundleData);
				m_TreeView.Reload();
				m_TreeView.OnSelectionChanged += SelectionChanged;
			}

			m_TreeView.OnGUI(new Rect(0, 20, position.width / 2, position.height - 20));
			GUILayout.BeginArea(new Rect(position.width / 2, 20, position.width / 2, position.height - 20));
			List<AssetImporter> importers = null;
			if (selectedItems.Count > 0)
			{
				string[] raw_bundles = AssetDatabase.GetAllAssetBundleNames();
				List<string> bundles = new List<string>();
				List<string> variants = new List<string>();
				foreach (var b in raw_bundles)
				{
					int index = b.IndexOf('.');
					if (index > 0)
					{
						string bn = b.Substring(0, index);
						if (!bundles.Contains(bn))
							bundles.Add(bn);
						string v = b.Substring(index + 1);
						if (!variants.Contains(v))
							variants.Add(v);
					}
					else
					{
						bundles.Add(b);
					}
				}
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
						foreach (var r in asset.references)
							GUILayout.Label("\t" + asset.GetFullReferencePath(r), richTextStyle);
						GUILayout.EndVertical();
					}
					if (asset.type == AssetBundleData.AssetInfo.Type.Bundle)
					{
						GUILayout.Label(asset.displayPath, richTextStyle);
						GUILayout.BeginVertical();
						foreach (var r in asset.rootDependencies)
							GUILayout.Label("<color=white>" + r + "</color>", richTextStyle);
						GUILayout.EndVertical();
					}
					GUILayout.BeginVertical();
					foreach (var r in i.data.issues)
						GUILayout.Label("<color=red>" + r + "</color>", richTextStyle);
					GUILayout.EndVertical();
				}
				EditorGUILayout.EndScrollView();

				if (importers != null)
				{
					GUILayout.BeginHorizontal();
					GUILayout.Label("<b><color=white>AssetBundle</color></b>", richTextStyle);
					string bid = importers[0].assetBundleName;
					string vid = importers[0].assetBundleVariant;
					foreach (var i in importers)
					{
						if (i.assetBundleName != bid)
							bid = "-";
						if (i.assetBundleVariant != vid)
							vid = "-";
					}
					bundles.Insert(0, "none");
					variants.Insert(0, "none");
					int currentVariantId = variants.IndexOf(vid);
					int currentBundleId = bundles.IndexOf(bid);
					int bundleId = EditorGUILayout.Popup(currentBundleId, bundles.ToArray());
					int variantId = EditorGUILayout.Popup(currentVariantId, variants.ToArray());
					if (bundleId != currentBundleId)
					{
						if (EditorUtility.DisplayDialog("AssetBundle Change", "Move selected assets to bundle " + bundles[bundleId] + "?", "Ok", "Cancel"))
						{
							foreach (var i in importers)
							{

								i.assetBundleName = bundleId == 0 ? string.Empty : bundles[bundleId];
								
							}
							ResetData();
						}
					}
					if (variantId != currentVariantId)
					{
						if (EditorUtility.DisplayDialog("AssetBundle Change", "Move selected assets to variant " + variants[variantId] + "?", "Ok", "Cancel"))
						{
							foreach (var i in importers)
							{
								i.assetBundleVariant = variantId == 0 ? string.Empty : variants[variantId];
							}
							ResetData();
						}
					}
					GUILayout.EndHorizontal();
				}
			}
			GUILayout.EndArea();
		}


		public void ResetData()
		{
			selectedItems.Clear();
			m_TreeViewState = null;
			m_TreeView = null;
			Repaint();
		}


		void SelectionChanged(IList<TreeViewItem> items)
		{
			selectedItems.Clear();
			foreach (var i in items)
				selectedItems.Add(i as AssetBundleBrowserTree.TreeItem);
		}
	}
}