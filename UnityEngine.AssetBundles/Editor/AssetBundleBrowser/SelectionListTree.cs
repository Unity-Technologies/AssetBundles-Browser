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
        /*
		internal class Item : TreeViewItem
		{
            AssetBundleState.AssetData m_data;
            public Item(AssetBundleState.AssetData bd) : base(bd.m_id, 0, bd.m_assetPath)
			{
				m_data = bd;
				icon = AssetDatabase.GetCachedIcon(m_data.m_assetPath) as Texture2D;
            }
		}
        */
        //AssetBundleState.AssetData m_selection;
        //   int m_selection = -1;

        public SelectionListTree(TreeViewState state) : base(state)
        {
            showBorder = true;
            Reload();
        }

        protected override void BuildRootAndRows(out TreeViewItem root, out IList<TreeViewItem> rows)
        {
            root = new TreeViewItem(-1, -1);
            rows = new List<TreeViewItem>();
            rows.Add(root);
            //       if (m_selection >= 0)
            if (m_selecteditems != null)
            {
                foreach (var a in m_selecteditems)
                {

                    var item = new TreeViewItem(a.name.GetHashCode(), 0, root, a.name);
                    item.userData = a;
                    item.icon = AssetDatabase.GetCachedIcon(a.name) as Texture2D;
                    rows.Add(item);
                    root.AddChild(item);
                    //show all references...
                }
            }
            // SetupParentsAndChildrenFromDepths(root, rows);
        }
        /*
        internal void SetItems(IList<int> list)
		{
            if(HasSelection())
                SetSelection(new List<int>());
            m_selection = -1;
            if (list.Count > 0)
                m_selection = list[0];
			Reload();
		}
        */
        internal void Clear()
        {
            m_selecteditems = null;
            Reload();
        }
        IEnumerable<AssetBundleState.AssetInfo> m_selecteditems;
        internal void SetItems(IEnumerable<AssetBundleState.AssetInfo> items)
        {
            if (HasSelection())
                SetSelection(new List<int>());
            m_selecteditems = items;
            Reload();
        }
    }
}
