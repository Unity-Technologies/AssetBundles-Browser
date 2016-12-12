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
		//	Reload();
		}

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item.displayName != AssetBundleState.NoBundleName;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (args.newName.Length > 0 && args.newName != args.originalName && !AssetBundleState.bundles.ContainsKey(args.newName))
            {
                var bi = TreeViewUtility.FindItem(args.itemID, rootItem).userData as AssetBundleState.BundleInfo;
                args.acceptedRename = true;
                AssetBundleState.RenameBundle(bi, args.newName);
            }
            else
            {
                args.acceptedRename = false;
            }
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            root.children = new List<TreeViewItem>();

            foreach (var b in AssetBundleState.bundles)
            {
                if (b.Key == AssetBundleState.NoBundleName)
                    continue;
                var item = new TreeViewItem(b.Value.name.GetHashCode(), 0, root, b.Key);
                item.icon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName) as Texture2D;
                item.userData = b.Value;
                root.AddChild(item);
                if (b.Key == AssetBundleState.editBundleName)
                    BeginRename(item, .25f);
            }
            AssetBundleState.editBundleName = string.Empty;

            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
		{
            m_assetList.SetSelectedBundle(selectedIds.Count == 0 ? null : TreeViewUtility.FindItem(selectedIds[0], rootItem).userData as AssetBundleState.BundleInfo);
        }

        protected override void ContextClickedItem(int id)
        {
            GenericMenu menu = new GenericMenu();
            var i = TreeViewUtility.FindItem(id, rootItem);
            if (i != null)
            {
                menu.AddItem(new GUIContent("Delete " + i.displayName), false, DeleteBundle, i.userData);
            }
            else
            {
                menu.AddItem(new GUIContent("New Bundle"), false, DeleteBundle, null);
            }
            menu.ShowAsContext();
        }

        void NewBundle(object o)
        {
            var bi = AssetBundleState.CreateEmptyBundle("New Bundle", true);
            Reload();
        }

        void DeleteBundle(object b)
        {
            AssetBundleState.DeleteBundle(b as AssetBundleState.BundleInfo);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.performDrop)
            {
                var targetBundle = args.parentItem.userData as AssetBundleState.BundleInfo;
                if (targetBundle != null)
                {
                    AssetBundleState.MoveAssetsToBundle(targetBundle, DragAndDrop.paths.Select(a => AssetBundleState.assets[a]));
                    SelectionChanged(GetSelection());
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
