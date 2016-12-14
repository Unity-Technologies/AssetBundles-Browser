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
            var root = new TreeViewItem(-1, -1);
            root.children = new List<TreeViewItem>();

            if (m_selecteditems != null)
            {
                int index = 0;
                foreach (var a in m_selecteditems)
                {
                    var item = new TreeViewItem(a.name.GetHashCode(), 0, root, a.name);
                    item.userData = a;
                    item.icon = AssetDatabase.GetCachedIcon(a.name) as Texture2D;
                    root.AddChild(item);
                    var refs = new List<AssetBundleState.AssetInfo>();
                    a.GatherReferences(refs);
                    if (refs.Count > 0)
                    {
                        var refItem = new TreeViewItem(index++, 1, refs.Count + " reference" + (refs.Count == 1 ? "" : "s"));
                        refItem.icon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName) as Texture2D;
                        item.AddChild(refItem);

                        foreach (var d in refs)
                        {
                            var di = new TreeViewItem(d.name.GetHashCode(), 2, d.name);
                            di.icon = AssetDatabase.GetCachedIcon(d.name) as Texture2D;
                            di.userData = d;
                            refItem.AddChild(di);
                        }
                    }

                    var bundles = new List<AssetBundleState.BundleInfo>();
                    a.GatherBundles(bundles);
                    if (bundles.Count > 0)
                    {
                        var refItem = new TreeViewItem(index++, 1, bundles.Count + " bundle" + (bundles.Count == 1 ? "" : "s"));
                        refItem.icon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName) as Texture2D;
                        item.AddChild(refItem);

                        foreach (var d in bundles)
                        {
                            var di = new TreeViewItem(d.name.GetHashCode(), 2, d.name);
                            di.icon = AssetDatabase.GetCachedIcon(d.name) as Texture2D;
                            di.userData = d;
                            refItem.AddChild(di);
                        }
                    }

                }
            }
            return root;
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

        internal void SetItems(IEnumerable<AssetBundleState.AssetInfo> items)
        {
            if (HasSelection())
                SetSelection(new List<int>());
            m_selecteditems = items;
            Reload();
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            foreach (var o in GetRowsFromIDs(args.draggedItemIDs).Select(a => a.userData))
                if (!(o is AssetBundleState.AssetInfo))
                    return false;
            args.draggedItemIDs = GetSelection();
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.paths = GetRowsFromIDs(args.draggedItemIDs).Select(a => a.userData == null ? string.Empty : (a.userData as AssetBundleState.AssetInfo).name).ToArray();
            DragAndDrop.StartDrag("SelectionListTree");
        }

        protected override void ContextClickedItem(int id)
        {
            AssetBundleState.ShowAssetContextMenu(GetRowsFromIDs(GetSelection()).Select(a => (a.userData as AssetBundleState.AssetInfo)));
        }
    }
}
