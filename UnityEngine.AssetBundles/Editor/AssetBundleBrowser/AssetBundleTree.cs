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
			Reload();
		}

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return item.displayName != "<none>";
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

        protected override void BuildRootAndRows(out TreeViewItem root, out IList<TreeViewItem> rows)
		{
			root = new TreeViewItem(-1, -1);
			rows = new List<TreeViewItem>();
            rows.Add(root);

            foreach(var b in AssetBundleState.bundles)
            {
                TreeViewItem item = new TreeViewItem(b.Value.name.GetHashCode(), 0, root, b.Key);
                item.icon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName) as Texture2D;
                item.userData = b.Value;
                rows.Add(item);
                root.AddChild(item);
            }
		}

        protected override void SelectionChanged(IList<int> selectedIds)
		{
            m_assetList.SetSelectedBundles(GetRowsFromIDs(selectedIds).Select(a => (a.userData as AssetBundleState.BundleInfo)));
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
            var bi = AssetBundleState.CreateEmptyBundle("New Bundle");
            Reload();
            foreach (var r in GetRows())
                if (r.displayName == "New Bundle")
                    BeginRename(r);
        }

        void DeleteBundle(object b)
        {
            AssetBundleState.DeleteBundle(b as AssetBundleState.BundleInfo);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.performDrop)
            {
                List<AssetBundleState.AssetInfo> draggedItems = new List<AssetBundleState.AssetInfo>();
                foreach (var a in DragAndDrop.paths)
                    draggedItems.Add(AssetBundleState.assets[a]);
                var targetBundle = args.parentItem.userData as AssetBundleState.BundleInfo;
                AssetBundleState.MoveAssetsToBundle(targetBundle, draggedItems);
                SelectionChanged(GetSelection());
            }
            return DragAndDropVisualMode.Move;
        }
        
    }
}
