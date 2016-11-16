using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using System;


namespace UnityEngine.AssetBundles
{
	internal class AssetListTree : TreeView
	{
        public class Item : TreeViewItem
        {
            public AssetBundleDataCache.AssetData m_data;
            public Item(AssetBundleDataCache.AssetData bd, int depth) : base(bd.m_id, depth, bd.m_displayName)
            {
                m_data = bd;
                icon = AssetDatabase.GetCachedIcon(m_data.m_assetPath) as Texture2D;
            }
        }

        List<AssetBundleDataCache.AssetData> m_assetsInSelectedBundles = new List<AssetBundleDataCache.AssetData>();
        SelectionListTree m_selectionList;

		public AssetListTree(TreeViewState state, SelectionListTree selList) : base(state)
		{
            m_selectionList = selList;
            Reload();
		}

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void BuildRootAndRows(out TreeViewItem root, out IList<TreeViewItem> rows)
		{
			root = new TreeViewItem(-1, -1);
			rows = new List<TreeViewItem>();

            foreach (var b in m_assetsInSelectedBundles)
                CreateItems(rows, b, 0);

            SetupParentsAndChildrenFromDepths(root, rows);
		}
        void CreateItems(IList<TreeViewItem> rows, AssetBundleDataCache.AssetData a, int depth)
        {
            Item item = new Item(a, depth);
            rows.Add(item);
            var dependencies = AssetDatabase.GetDependencies(a.m_assetPath, false);
            if (IsExpanded(a.m_id))
            {
                foreach (var d in dependencies)
                {
                    if (d != a.m_assetPath)
                    {
                        AssetBundleDataCache.AssetData ad = AssetBundleDataCache.GetAssetData(string.Empty, d);
                        if (string.IsNullOrEmpty(ad.m_bundle))
                            CreateItems(rows, ad, depth + 1);
                    }
                }
            }
            else
            {
                if(dependencies.Length > 0 && dependencies[0] != a.m_assetPath)
                    item.children = CreateChildListForCollapsedParent();
            }
        }

        IList<TreeViewItem> m_selectedBundleslist = null;
        internal void SetItems(IList<TreeViewItem> list)
		{
            m_selectedBundleslist = list;
            if (HasSelection())
                SetSelection(AssetBundleDataCache.s_emptyIntList);
            m_selectionList.Clear();
			m_assetsInSelectedBundles.Clear();

            foreach (var i in list)
			{
				foreach (var a in (i as AssetBundleTree.Item).data.assets)
				{
					if (m_assetsInSelectedBundles.Find(s => s.m_assetPath == a.m_assetPath) == null)
						m_assetsInSelectedBundles.Add(a);
				}
			}
			Reload();
        }

        protected override void ContextClickedItem(int id)
        {
            var i = TreeViewUtility.FindItem(id, rootItem);
            if (i != null)
            {
                GenericMenu menu = new GenericMenu();
                foreach(var b in AssetBundleDataCache.s_bundleDataMap.Keys)
                    menu.AddItem(new GUIContent("Move to bundle/" +  b), false, OnContextMenuFindIssues, b);
                menu.ShowAsContext();
            }
        }

        void OnContextMenuFindIssues(object target)
        {
            foreach (var t in GetRowsFromIDs(GetSelection()))
            {
                Item i = t as Item;
                AssetImporter importer = AssetImporter.GetAtPath(i.m_data.m_assetPath);
                string bundleName = target as string;
                int dot = bundleName.IndexOf('.');
                if (dot < 0)
                {
                    importer.assetBundleName = bundleName;
                    importer.assetBundleVariant = string.Empty;
                }
                else
                {
                    importer.assetBundleName = bundleName.Substring(0, dot);
                    importer.assetBundleVariant = bundleName.Substring(dot + 1);
                }
            }
            SetItems(m_selectedBundleslist);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
		{
            m_selectionList.SetItems(GetRowsFromIDs(selectedIds));
		}
	}
}
