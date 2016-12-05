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
        IEnumerable<AssetBundleState.BundleInfo> m_selectedBundles;
        HashSet<AssetBundleState.AssetInfo> m_assetsInSelectedBundles = new HashSet<AssetBundleState.AssetInfo>();
        SelectionListTree m_selectionList;

		public AssetListTree(TreeViewState state, SelectionListTree selList) : base(state)
		{
            m_selectionList = selList;
            Reload();
		}

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            foreach (var a in m_assetsInSelectedBundles)
            {
                var item = new TreeViewItem(a.name.GetHashCode(), 0, root, System.IO.Path.GetFileNameWithoutExtension(a.name));
                item.userData = a;
                item.icon = AssetDatabase.GetCachedIcon(a.name) as Texture2D;
                root.AddChild(item);
            }
            return root;
        }

        protected override void DoubleClickedItem(int id)
        {
            var assetInfo = TreeViewUtility.FindItem(id, rootItem).userData as AssetBundleState.AssetInfo;
			if (assetInfo != null)
			{
				Object o = AssetDatabase.LoadAssetAtPath<Object>(assetInfo.name);
				EditorGUIUtility.PingObject(o);
				Selection.activeObject = o;
			}
        }


        internal void SetSelectedBundles(IEnumerable<AssetBundleState.BundleInfo> b)
        {
            m_selectedBundles = b;
            m_assetsInSelectedBundles.Clear();
            if (HasSelection())
                SetSelection(new List<int>());

            foreach (var bundleInfo in m_selectedBundles)
                foreach (var a in bundleInfo.assets)
                    m_assetsInSelectedBundles.Add(a);
            Reload();
            SelectionChanged(GetSelection());
        }

        protected override void ContextClickedItem(int id)
        {
            var i = TreeViewUtility.FindItem(id, rootItem);
            if (i != null)
            {
                GenericMenu menu = new GenericMenu();
                foreach(var b in AssetBundleState.bundles)
                    if(!m_selectedBundles.Contains(b.Value))
                        menu.AddItem(new GUIContent("Move to bundle/" + b.Key), false, MoveToBundle, b.Value);
                menu.AddItem(new GUIContent("Move to bundle/<Create New Bundle...>"), false, MoveToBundle, null);
                menu.ShowAsContext();
            }
        }

        void MoveToBundle(object target)
        {
            AssetBundleState.BundleInfo bi = target as AssetBundleState.BundleInfo;
            if (bi == null)
                bi = AssetBundleState.CreateEmptyBundle("Bundle" + Random.Range(0, 10000));

            AssetBundleState.MoveAssetsToBundle(bi, GetRowsFromIDs(GetSelection()).Select(a => a.userData as AssetBundleState.AssetInfo));
            SetSelectedBundles(m_selectedBundles);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
		{
            m_selectionList.SetItems(GetRowsFromIDs(GetSelection()).Select(a => a.userData as AssetBundleState.AssetInfo));
		}

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            args.draggedItemIDs = GetSelection();
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.paths = GetRowsFromIDs(args.draggedItemIDs).Select(a => (a.userData as AssetBundleState.AssetInfo).name).ToArray();
            DragAndDrop.StartDrag("AssetListTree");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (args.performDrop)
            {
                var targetBundle = (args.parentItem.userData as AssetBundleState.AssetInfo).bundle;
                if (targetBundle != null)
                {
                    AssetBundleState.MoveAssetsToBundle(targetBundle, DragAndDrop.paths.Select(a => AssetBundleState.assets[a]));
                }
            }
            return DragAndDropVisualMode.Move;
        }
    }


}
