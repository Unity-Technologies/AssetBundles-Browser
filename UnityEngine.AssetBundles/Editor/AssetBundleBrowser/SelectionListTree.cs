using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using System;


namespace UnityEngine.AssetBundles
{
	internal class SelectionListTree : TreeView
	{
		internal class Item : TreeViewItem
		{
            AssetBundleDataCache.AssetData m_data;
            public Item(AssetBundleDataCache.AssetData bd) : base(bd.m_id, 0, bd.m_assetPath)
			{
				m_data = bd;
				icon = AssetDatabase.GetCachedIcon(m_data.m_assetPath) as Texture2D;
            }
		}
        AssetBundleDataCache.AssetData m_selection;

        public SelectionListTree(TreeViewState state) : base(state)
		{
            showBorder = true;
			Reload();
		}

        protected override void BuildRootAndRows(out TreeViewItem root, out IList<TreeViewItem> rows)
		{
			root = new TreeViewItem(-1, -1);
			rows = new List<TreeViewItem>();
            if (m_selection != null)
            {
                rows.Add(new Item(m_selection));
                //show all references...
            }

            SetupParentsAndChildrenFromDepths(root, rows);
		}

        internal void SetItems(IList<TreeViewItem> list)
		{
            if(HasSelection())
                SetSelection(AssetBundleDataCache.s_emptyIntList);
            m_selection = null;
            if (list.Count > 0)
                m_selection = (list[0] as AssetListTree.Item).m_data;
			Reload();
		}

        internal void Clear()
        {
            m_selection = null;
            Reload();
        }
    }
}
