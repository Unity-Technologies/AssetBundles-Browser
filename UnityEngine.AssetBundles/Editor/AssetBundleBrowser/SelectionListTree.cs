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

            if (m_selecteditems != null)
            {
                int index = 0;
                foreach (var a in m_selecteditems)
                {
                    var item = new TreeViewItem(a.name.GetHashCode(), 0, root, a.name);
                    item.userData = a;
                    item.icon = AssetDatabase.GetCachedIcon(a.name) as Texture2D;
                    rows.Add(item);
                    var deps = AssetDatabase.GetDependencies(a.name);
                    if (deps.Length > 1)
                    {
                        if (IsExpanded(item.id))
                        {
                            var refItem = new TreeViewItem(index, 1, (deps.Length - 1) + " dependenc" + (deps.Length == 2 ? "y" : "ies"));
                            refItem.icon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName) as Texture2D;
                            rows.Add(refItem);
                            if (IsExpanded(index))
                            {
                                foreach (var d in deps)
                                {
                                    if (d != a.name)
                                    {
                                        var di = new TreeViewItem(d.GetHashCode(), 2, d);
                                        di.icon = AssetDatabase.GetCachedIcon(d) as Texture2D;
                                        di.userData = AssetBundleState.assets[d];
                                        rows.Add(di);
                                    }
                                }
                            }
                            else
                            {
                                refItem.children = CreateChildListForCollapsedParent();
                            }
                            index++;
                        }
                        else
                        {
                            item.children = CreateChildListForCollapsedParent();
                        }
                    }
                }
            }
            SetupParentsAndChildrenFromDepths(root, rows);
        }

        protected override void DoubleClickedItem(int id)
        {
            var assetInfo = TreeViewUtility.FindItem(id, rootItem).userData as AssetBundleState.AssetInfo;
            if (assetInfo != null)
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(assetInfo.name);
        }

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
