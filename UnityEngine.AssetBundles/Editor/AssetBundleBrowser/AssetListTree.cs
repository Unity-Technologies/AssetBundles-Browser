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
        DisplayData m_data;
        SelectionListTree m_selectionList;

        class DisplayData
        {
            public AssetBundleState.BundleInfo m_bundle;
            public List<AssetBundleState.AssetInfo> m_assets;
            public List<AssetBundleState.AssetInfo> m_extendedAssets;
            int index = -1;
            public DisplayData(AssetBundleState.BundleInfo b)
            {
                m_bundle = b;
                m_assets = new List<AssetBundleState.AssetInfo>(m_bundle.m_assets.Values);
                m_extendedAssets = new List<AssetBundleState.AssetInfo>();
            }

            
            public bool Update()
            {
                if (index >= m_assets.Count)
                    return false;
                int count = m_extendedAssets.Count;
                if(index >= 0)
                    m_bundle.GatherDependencies(m_assets[index], m_extendedAssets);
                index++;
                return count != m_extendedAssets.Count;
            }

        }

		public AssetListTree(TreeViewState state, SelectionListTree selList) : base(state)
		{
            m_selectionList = selList;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            DefaultStyles.label.richText = true;
        }

        float lastUpdateTime = 0;
        float updateDelay = 0;
        bool needsReload = false;
        public void Update()
        {
            if (m_data != null)
            {
                if(Time.realtimeSinceStartup - updateDelay > .1f)
                    needsReload |= m_data.Update();

                if (needsReload && Time.realtimeSinceStartup - lastUpdateTime > .3f)
                {
                    Reload();
                    lastUpdateTime = Time.realtimeSinceStartup;
                    needsReload = false;
                }
            }
        }

        Color greyColor = Color.white * .75f;

        protected override TreeViewItem BuildRoot()
        {
            var root = new AssetBundleState.AssetInfo.TreeItem();
            root.children = new List<TreeViewItem>();
            if (m_data != null)
            {
                foreach (var a in m_data.m_assets)
                    root.AddChild(new AssetBundleState.AssetInfo.TreeItem(a, 0, Color.white));

                foreach (var a in m_data.m_extendedAssets)
                    root.AddChild(new AssetBundleState.AssetInfo.TreeItem(a, 0, greyColor));
            }
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            Color oldColor = GUI.color;
            GUI.color = (args.item as AssetBundleState.AssetInfo.TreeItem).color;
            base.RowGUI(args);
            GUI.color = oldColor;

        }

        protected override void DoubleClickedItem(int id)
        {
            var assetInfo = Utilities.FindItem<AssetBundleState.AssetInfo.TreeItem>(rootItem, id).asset;
			if (assetInfo != null)
			{
				Object o = AssetDatabase.LoadAssetAtPath<Object>(assetInfo.m_name);
				EditorGUIUtility.PingObject(o);
				Selection.activeObject = o;
			}
        }

        internal void SetSelectedBundle(AssetBundleState.BundleInfo b)
        {
            m_data = b == null ? null : new DisplayData(b);
            updateDelay = lastUpdateTime = Time.realtimeSinceStartup;
            needsReload = true;
            Reload();
        }

        IEnumerable<AssetBundleState.AssetInfo> GetSelectedAssets()
        {
            return GetAssets(GetSelection());
        }
        IEnumerable<AssetBundleState.AssetInfo> GetAssets(IList<int> ids)
        {
            return GetRowsFromIDs(ids).Select(a => (a as AssetBundleState.AssetInfo.TreeItem).asset);
        }

        protected override void ContextClickedItem(int id)
        {
            AssetBundleState.ShowAssetContextMenu(GetSelectedAssets());
        }

        protected override void SelectionChanged(IList<int> selectedIds)
		{
            m_selectionList.SetItems(GetSelectedAssets());
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            args.draggedItemIDs = GetSelection();
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.paths = GetAssets(args.draggedItemIDs).Select(a=>a.m_name).ToArray();
            DragAndDrop.StartDrag("AssetListTree");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (m_data == null)
                return DragAndDropVisualMode.Rejected;

            if (args.performDrop)
            {
                AssetBundleState.MoveAssetsToBundle(DragAndDrop.paths.Select(a => AssetBundleState.GetAsset(a)), m_data.m_bundle.m_name);
                SetSelectedBundle(m_data.m_bundle);
            }

            return DragAndDropVisualMode.Move;
        }

        protected override void KeyEvent()
        {
            if (m_data != null && Event.current.keyCode == KeyCode.Delete && GetSelection().Count > 0)
            {
                AssetBundleState.MoveAssetsToBundle(GetSelectedAssets(), string.Empty);
                SetSelectedBundle(m_data.m_bundle);
                Event.current.Use();
            }
        }
    }
}
