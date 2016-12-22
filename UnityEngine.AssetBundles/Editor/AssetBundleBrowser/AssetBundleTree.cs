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
            return false;
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
                AssetBundleState.RenameBundle(Utilities.FindItem<AssetBundleState.BundleInfo.TreeItem>(rootItem, args.itemID).bundle, args.newName);
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
                var item = new AssetBundleState.BundleInfo.TreeItem(b.Value, 0);
                root.AddChild(item);
                if (b.Key == AssetBundleState.m_editBundleName)
                    BeginRename(item, .25f);
            }
            AssetBundleState.m_editBundleName = string.Empty;

            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
		{
            if (selectedIds.Count == 0)
            {
                m_assetList.SetSelectedBundle(null);
            }
            else
            {
                var item = Utilities.FindItem<AssetBundleState.BundleInfo.TreeItem>(rootItem, selectedIds[0]);
                m_assetList.SetSelectedBundle(item == null ? null : item.bundle);
            }
        }

        protected override void ContextClickedItem(int id)
        {
            GenericMenu menu = new GenericMenu();
            var bundle = Utilities.FindItem<AssetBundleState.BundleInfo.TreeItem>(rootItem, id).bundle;
            menu.AddItem(new GUIContent("Delete " + bundle.m_name), false, DeleteBundle, bundle);
            menu.ShowAsContext();
        }
  
        void DeleteBundle(object b)
        {
            AssetBundleState.RenameBundle(b as AssetBundleState.BundleInfo, string.Empty);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.dragAndDropPosition == DragAndDropPosition.UponItem)
            {
                if (args.performDrop)
                {
                    var targetBundle = (args.parentItem as AssetBundleState.BundleInfo.TreeItem).bundle;
                    if (targetBundle != null)
                    {
                        AssetBundleState.MoveAssetsToBundle(DragAndDrop.paths.Select(a => AssetBundleState.GetAsset(a)), targetBundle.m_name);
                        SelectionChanged(GetSelection());
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
        
        internal void Refresh()
        {
            Reload();
            SelectionChanged(GetSelection());
        }
    }
}
