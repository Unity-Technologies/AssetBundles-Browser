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
        IEnumerable<AssetBundleState.AssetInfo> m_selecteditems;

        public SelectionListTree(TreeViewState state) : base(state)
        {
            showBorder = true;
        }
        protected override TreeViewItem BuildRoot()
        {
            var root = new AssetBundleState.AssetInfo.TreeItem();
            root.children = new List<TreeViewItem>();

            if (m_selecteditems != null)
            {
                int index = 0;
                foreach (var a in m_selecteditems)
                {
                    var item = new AssetBundleState.AssetInfo.TreeItem(a, 0, a.m_name);
                    root.AddChild(item);
                    var refs = new List<AssetBundleState.AssetInfo>();
                    a.GatherReferences(refs);
                    if (refs.Count > 0)
                    {
                        var refItem = new TreeViewItem(index++, 1, refs.Count + " reference" + (refs.Count == 1 ? "" : "s"));
                        refItem.icon = Utilities.FoldlerIcon;
                        item.AddChild(refItem);

                        foreach (var d in refs)
                            refItem.AddChild(new AssetBundleState.AssetInfo.TreeItem(d, 2, d.m_name));
                    }

                    var bundles = new List<AssetBundleState.BundleInfo>();
                    a.GatherBundles(bundles);
                    if (bundles.Count > 0)
                    {
                        var refItem = new TreeViewItem(index++, 1, bundles.Count + " bundle" + (bundles.Count == 1 ? "" : "s"));
                        refItem.icon = Utilities.FoldlerIcon;
                        item.AddChild(refItem);

                        foreach (var d in bundles)
                            refItem.AddChild(new AssetBundleState.BundleInfo.TreeItem(d, 2));
                    }

                }
            }
            return root;
        }

        protected override void DoubleClickedItem(int id)
        {
            var assetInfo = Utilities.FindItem<AssetBundleState.AssetInfo.TreeItem>(rootItem, id);
            if (assetInfo is AssetBundleState.AssetInfo.TreeItem)
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(assetInfo.asset.m_name);
        }

        internal void Clear()
        {
            m_selecteditems = null;
            Reload();
        }

        internal void SetItems(IEnumerable<AssetBundleState.AssetInfo> items)
        {
            if (HasSelection())
                SetSelection(new List<int>());
            m_selecteditems = items;
            Reload();
        }

        IEnumerable<AssetBundleState.AssetInfo> GetAssets(IList<int> ids)
        {
            return new List<AssetBundleState.AssetInfo>(GetRowsFromIDs(ids).Select(a => (a as AssetBundleState.AssetInfo.TreeItem).asset));
        }

        bool VeryifyItemsAreAssets(IList<int> ids)
        {
            foreach (var o in GetRowsFromIDs(ids))
                if (!(o is AssetBundleState.AssetInfo.TreeItem))
                    return false;
            return true;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            if (!VeryifyItemsAreAssets(args.draggedItemIDs))
                return false;
            args.draggedItemIDs = GetSelection();
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.paths = GetAssets(args.draggedItemIDs).Select(a=>a.m_name).ToArray();
            DragAndDrop.StartDrag("SelectionListTree");
        }

        protected override void ContextClickedItem(int id)
        {
            if (VeryifyItemsAreAssets(GetSelection()))
                AssetBundleState.ShowAssetContextMenu(GetAssets(GetSelection()));
        }
    }
}
