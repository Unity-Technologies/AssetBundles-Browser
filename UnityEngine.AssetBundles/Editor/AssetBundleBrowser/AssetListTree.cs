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
        AssetBundleState.BundleInfo m_selectedBundle = null;
        SelectionListTree m_selectionList;

		public AssetListTree(TreeViewState state, SelectionListTree selList) : base(state)
		{
            m_selectionList = selList;
            //Reload();
		}

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            root.children = new List<TreeViewItem>();
            if (m_selectedBundle != null)
            {
                foreach (var a in m_selectedBundle.assets)
                {
                    var item = new TreeViewItem(a.name.GetHashCode(), 0, root, System.IO.Path.GetFileNameWithoutExtension(a.name));
                    item.userData = a;
                    item.icon = AssetDatabase.GetCachedIcon(a.name) as Texture2D;
                    root.AddChild(item);
                }
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


        internal void SetSelectedBundle(AssetBundleState.BundleInfo b)
        {
            if (HasSelection() && m_selectedBundle != b)
                SetSelection(new List<int>());
            m_selectedBundle = b;
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
                    if(b.Value != m_selectedBundle)
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
            SetSelectedBundle(m_selectedBundle);
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
            if (m_selectedBundle == null)
                return DragAndDropVisualMode.None;

            if (args.performDrop)
            {
                AssetBundleState.MoveAssetsToBundle(m_selectedBundle, DragAndDrop.paths.Select(a => AssetBundleState.GetAsset(a)));
                SetSelectedBundle(m_selectedBundle);
            }

            return DragAndDropVisualMode.Move;
        }

        protected override void KeyEvent()
        {
            if (Event.current.keyCode == KeyCode.Delete && GetSelection().Count > 0)
            {
                MoveToBundle(AssetBundleState.bundles[AssetBundleState.NoBundleName]);
                SetSelection(new List<int>());
                Event.current.Use();
            }
        }

    }


}
