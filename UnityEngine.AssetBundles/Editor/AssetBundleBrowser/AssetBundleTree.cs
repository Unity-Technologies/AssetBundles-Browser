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
            AssetBundleState.Rebuild();
            m_assetList = alt;
            showBorder = true;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return true;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item.displayName.Length > 0;
        }



        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (args.newName.Length > 0 && args.newName != args.originalName && !AssetBundleState.m_bundles.ContainsKey(args.newName))
            {
                args.acceptedRename = true;
                var node = Utilities.FindItem<AssetBundleState.BundleInfo.TreeItem>(rootItem, args.itemID);
                List<AssetBundleState.BundleInfo> bundles = new List<AssetBundleState.BundleInfo>();
                GatherAllBundlesFromNode(node, bundles);
                AssetBundleState.StartABMoveBatch();
                foreach (var b in bundles)
                    AssetBundleState.RenameBundle(b, b.m_name.Replace(args.originalName, args.newName));
                AssetBundleState.EndABMoveBatch();
            }
            else
            {
                args.acceptedRename = false;
            }
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new AssetBundleState.BundleInfo.TreeItem();
            root.children = new List<TreeViewItem>();

            foreach (var b in AssetBundleState.m_bundles)
            {
                if (b.Key.Length == 0)
                    continue;
               
                var parent = CreateFolder(root, b.Value.Folder);

                bool hasBundle = false;
                if (parent.hasChildren)
                {
                    foreach (var c in parent.children)
                    {
                        var childItem = c as AssetBundleState.BundleInfo.TreeItem;
                        if (childItem.GetPath() == b.Key)
                        {
                            childItem.bundle = b.Value;
                            hasBundle = true;
                            break;
                        }
                    }
                }
                if (hasBundle)
                    continue;
                var item = new AssetBundleState.BundleInfo.TreeItem(b.Value, parent.depth + 1);
                parent.AddChild(item);
                if (b.Key == AssetBundleState.m_editBundleName)
                    BeginRename(item, .25f);
            }
            AssetBundleState.m_editBundleName = string.Empty;

            return root;
        }

        private AssetBundleState.BundleInfo.TreeItem CreateFolder(AssetBundleState.BundleInfo.TreeItem p, string f)
        {
            if (f.Length == 0)
                return p;

            string[] folders = f.Split('/');
            int index = 0;
            while (index < folders.Length)
            {
                bool found = false;
                if (p.hasChildren)
                {
                    foreach (var c in p.children)
                    {
                        if (c.displayName == folders[index])
                        {
                            p = c as AssetBundleState.BundleInfo.TreeItem;
                            found = true;
                            break;
                        }
                    }
                }
                if (!found)
                {
                    var c = new AssetBundleState.BundleInfo.TreeItem((f + index).GetHashCode(), index, folders[index]);
                    p.AddChild(c);
                    p = c;
                }
                index++;
            }
            return p;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
		{
            List<AssetBundleState.BundleInfo> selectedBundles = new List<AssetBundleState.BundleInfo>();
            foreach (var i in selectedIds.Select(b => Utilities.FindItem<AssetBundleState.BundleInfo.TreeItem>(rootItem, b)))
                GatherAllBundlesFromNode(i, selectedBundles);
            m_assetList.SetSelectedBundles(selectedBundles);
        }

        protected override void ContextClickedItem(int id)
        {
            var selectedNodes = new List<AssetBundleState.BundleInfo.TreeItem>(GetSelection().Select(s => Utilities.FindItem<AssetBundleState.BundleInfo.TreeItem>(rootItem, s)));
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Move duplicated assets to new bundle"), false, DedupeBundles, selectedNodes);
            menu.AddItem(new GUIContent("Delete " + (selectedNodes.Count == 1 ? selectedNodes[0].GetPath() : "multiple bundles") ), false, DeleteBundles, selectedNodes);
            menu.ShowAsContext();
        }

        void DedupeBundles(object context)
        {
            var selectedNodes = context as List<AssetBundleState.BundleInfo.TreeItem>;
            List<AssetBundleState.BundleInfo> bundles = new List<AssetBundleState.BundleInfo>();
            foreach (var n in selectedNodes)
                GatherAllBundlesFromNode(n, bundles);
            var allAssets = new HashSet<string>();
            var duplicatedAssets = new List<AssetBundleState.AssetInfo>();
            foreach (var b in bundles)
            {
                var deps = new List<AssetBundleState.AssetInfo>();
                b.GatherImplicitDependencies(deps);
                foreach (var d in deps)
                {
                    if (allAssets.Contains(d.m_name))
                        duplicatedAssets.Add(d);
                    else
                        allAssets.Add(d.m_name);
                }
            }

            if (duplicatedAssets.Count > 0)
            {
                AssetBundleState.StartABMoveBatch();
                AssetBundleState.MoveAssetsToBundle(duplicatedAssets, null);
                AssetBundleState.EndABMoveBatch();
            }
        }

        void DeleteBundles(object b)
        {
            var selectedNodes = b as List<AssetBundleState.BundleInfo.TreeItem>;
            List<AssetBundleState.BundleInfo> bundles = new List<AssetBundleState.BundleInfo>();
            foreach(var n in selectedNodes)
                GatherAllBundlesFromNode(n, bundles);
            var sb = new System.Text.StringBuilder();
            foreach (var r in bundles)
                sb.AppendLine(r.m_name);
            if (EditorUtility.DisplayDialog("Bundle delete confirmation", "Do you want to delete these bundles:" + Environment.NewLine + sb.ToString(), "Yes", "No"))
            {
                AssetBundleState.StartABMoveBatch();
                foreach (var r in bundles)
                    AssetBundleState.RenameBundle(r, string.Empty);
                AssetBundleState.EndABMoveBatch();
            }

        }


        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.dragAndDropPosition == DragAndDropPosition.UponItem)
            {
                if (args.performDrop)
                {
                    var targetNode = args.parentItem as AssetBundleState.BundleInfo.TreeItem;
                    var targetBundle = targetNode.bundle;
                    var draggedNodes = DragAndDrop.GetGenericData("AssetBundleState.BundleInfo.TreeItem") as List<AssetBundleState.BundleInfo.TreeItem>;
                    if (draggedNodes != null)
                    {
                        foreach (var draggedNode in draggedNodes)
                        {
                            var res = new List<AssetBundleState.BundleInfo>();
                            GatherAllBundlesFromNode(draggedNode, res);
                            AssetBundleState.StartABMoveBatch();
                            foreach (var b in res)
                            {
                                var dstBundle = targetNode.GetPath() + "/" + b.m_name.Substring(b.m_name.IndexOf(draggedNode.displayName));
                                AssetBundleState.MoveAssetsToBundle(b.m_assets.Values, dstBundle);
                            }
                            AssetBundleState.EndABMoveBatch();

                            foreach (var b in res)
                                AssetBundleState.RemoveBundle(b.m_name);
                            AssetBundleState.RemoveBundle(draggedNode.GetPath());
                        }
                        Reload();
                    }
                    else if(DragAndDrop.paths != null)
                    {
                        AssetBundleState.StartABMoveBatch();
                        AssetBundleState.MoveAssetsToBundle(DragAndDrop.paths.Select(a => AssetBundleState.GetAsset(a)), targetNode.GetPath());
                        AssetBundleState.EndABMoveBatch();
                    }
                }
                return DragAndDropVisualMode.Move;
            }
            else
            {
                if (args.performDrop)
                {
                    AssetBundleState.StartABMoveBatch();
                    foreach (var a in DragAndDrop.paths)
                    {
                        if (AssetDatabase.GetMainAssetTypeAtPath(a) == typeof(SceneAsset))
                        {
                            var bundle = AssetBundleState.GetBundle(System.IO.Path.GetFileNameWithoutExtension(a).ToLower());
                            AssetBundleState.MoveAssetsToBundle(new AssetBundleState.AssetInfo[] { AssetBundleState.GetAsset(a)}, bundle.m_name);
                        }
                    }
                    AssetBundleState.EndABMoveBatch();
                    return DragAndDropVisualMode.Move;
                }
            }
            return DragAndDropVisualMode.Move;
        }

        private void GatherAllBundlesFromNode(AssetBundleState.BundleInfo.TreeItem item, List<AssetBundleState.BundleInfo> res)
        {
            if (item == null)
                return;
            if (item.bundle != null)
                res.Add(item.bundle);
            if (!item.hasChildren)
                return;
            foreach (var c in item.children)
                GatherAllBundlesFromNode(c as AssetBundleState.BundleInfo.TreeItem, res);
        }
        private void GatherAllNodes(AssetBundleState.BundleInfo.TreeItem item, List<AssetBundleState.BundleInfo.TreeItem> res)
        {
            res.Add(item);
            if (!item.hasChildren)
                return;
            foreach (var c in item.children)
                GatherAllNodes(c as AssetBundleState.BundleInfo.TreeItem, res);
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            var selectedBundles = new List<AssetBundleState.BundleInfo.TreeItem>();
            foreach (var i in args.draggedItemIDs.Select(b => Utilities.FindItem<AssetBundleState.BundleInfo.TreeItem>(rootItem, b)))
                GatherAllNodes(i, selectedBundles);
            DragAndDrop.SetGenericData("AssetBundleState.BundleInfo.TreeItem", selectedBundles);
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            DragAndDrop.StartDrag("AssetBundleTree");
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            args.draggedItemIDs = GetSelection();
            return true;
        }

        internal void Refresh()
        {
            Reload();
            SelectionChanged(GetSelection());
        }
    }
}
