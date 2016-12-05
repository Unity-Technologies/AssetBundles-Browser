using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;

namespace UnityEngine.AssetBundles
{
	internal class AssetBundleChangesWindow : EditorWindow
	{
		[SerializeField]
		TreeViewState m_treeState;
		TreeView m_tree;
		
		internal static void ShowWindow()
		{
			var window = GetWindow<AssetBundleChangesWindow>();
			window.titleContent = new GUIContent("Asset Bundle Changes");
			window.Show();
		}

		class ModTree : TreeView
		{
			public ModTree(TreeViewState s) : base(s)
			{
			}

			protected override TreeViewItem BuildRoot()
			{
				var root = new TreeViewItem(-1, -1);
				foreach (var a in AssetBundleState.modifications)
				{
					var item = new TreeViewItem(a.GetType().Name.GetHashCode(), 0, root, a.GetDisplayString());
					item.userData = a;
					root.AddChild(item);
					IEnumerable<string> subItems = a.GetDisplaySubStrings();
					if (subItems != null)
					{
						foreach (var si in subItems)
							item.AddChild(new TreeViewItem(si.GetHashCode(), 1, item, si));
					}
				}
				return root;
			}
		}

		void OnGUI()
		{
			if (m_treeState == null)
			{
				m_treeState = new TreeViewState();
				m_tree = new ModTree(m_treeState);
				m_tree.Reload();
			}

			GUILayout.BeginVertical();
			m_tree.OnGUI(new Rect(0, 0, position.width, position.height - 25));
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Apply Changes"))
			{
				AssetBundleState.ApplyChanges();
				Close();
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}
	}
}
