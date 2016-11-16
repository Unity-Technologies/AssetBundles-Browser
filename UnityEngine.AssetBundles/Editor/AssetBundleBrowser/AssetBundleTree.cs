using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using System;

namespace UnityEngine.AssetBundles
{
	internal class AssetBundleTree : TreeView
	{
        
		AssetListTree m_assetList;

		public AssetBundleTree(TreeViewState state, AssetListTree alt) : base(state)
		{
			m_assetList = alt;
            AssetBundleDataCache.InitializeBundleData(AssetDatabase.GetAllAssetBundleNames());
			Reload();
		}

        protected override void ContextClickedItem(int id)
        {
            var i = TreeViewUtility.FindItem(id, rootItem);
            if (i != null)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Find issues"), false, OnContextMenuFindIssues, i);
                menu.ShowAsContext();
            }
        }

        void OnContextMenuFindIssues(object target)
        {
            foreach (var t in GetRowsFromIDs(GetSelection()))
            {
                Item i = t as Item;

            }
        }

        public class Item : TreeViewItem
		{
			public AssetBundleDataCache.BundleData data;
			public Item(AssetBundleDataCache.BundleData bd) : base(bd.m_id, bd.depth, bd.fullName)
			{
				data = bd;
				icon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName) as Texture2D;
			}
		}

		protected override void BuildRootAndRows(out TreeViewItem root, out IList<TreeViewItem> rows)
		{
			root = new Item(AssetBundleDataCache.s_bundleData);
			rows = new List<TreeViewItem>();

			foreach (var c in AssetBundleDataCache.s_bundleData.m_children)
				CreateBundleTreeItems(rows, c);

			SetupParentsAndChildrenFromDepths(root, rows);
		}

		private void CreateBundleTreeItems(IList<TreeViewItem> rows, AssetBundleDataCache.BundleData bundleData)
		{
			TreeViewItem item = new Item(bundleData);
			rows.Add(item);
			if (bundleData.m_children.Count > 0)
			{
				if (IsExpanded(bundleData.m_id))
				{
					foreach (var c in bundleData.m_children)
						CreateBundleTreeItems(rows, c);
				}
				else
				{
					item.children = CreateChildListForCollapsedParent();
				}
			}
		}

		protected override void SelectionChanged(IList<int> selectedIds)
		{
			m_assetList.SetItems(GetRowsFromIDs(selectedIds));
		}
	}
}
