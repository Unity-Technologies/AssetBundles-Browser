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

        class AssetColumn : MultiColumnHeaderState.Column
        {
            public AssetColumn(string label)
            {
                headerContent = new GUIContent(label, label + " tooltip");
                minWidth = 50;
                width = 100;
                maxWidth = 200;
                headerTextAlignment = TextAlignment.Left;
                canSort = true;
            }
        }

        public static MultiColumnHeaderState.Column[] GetColumns()
        {
            return new MultiColumnHeaderState.Column[] { new AssetColumn("Asset"), new AssetColumn("Bundle"), new AssetColumn("Size") };
        }

        class DisplayData
        {
            public IEnumerable<AssetBundleState.BundleInfo> m_bundles;
            public List<AssetBundleState.AssetInfo> m_assets;
            public List<AssetBundleState.AssetInfo> m_extendedAssets;
            int index = -1;
            public DisplayData(IEnumerable<AssetBundleState.BundleInfo> bundles)
            {
                m_bundles = bundles;
                m_assets = new List<AssetBundleState.AssetInfo>();
                foreach (var b in bundles)
                    m_assets.AddRange(b.m_assets.Values);
                m_extendedAssets = new List<AssetBundleState.AssetInfo>();
            }

            
            public bool Update()
            {
                if (index >= m_assets.Count)
                    return false;
                int count = m_extendedAssets.Count;
                if (index >= 0)
                    m_assets[index].m_bundle.GatherDependencies(m_assets[index], m_extendedAssets);
                index++;
                return count != m_extendedAssets.Count;
            }

        }

		public AssetListTree(TreeViewState state, MultiColumnHeaderState mchs, SelectionListTree selList) : base(state, new MultiColumnHeader(mchs))
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

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
           // SortIfNeeded(root, rows);
            return rows;
        }


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
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), args.item as AssetBundleState.AssetInfo.TreeItem, args.GetColumn(i), ref args);
            GUI.color = oldColor;
        }

        private void CellGUI(Rect cellRect, AssetBundleState.AssetInfo.TreeItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            GUI.color = item.color;
            switch (column)
            {
                case 0:
                    {
                        var iconRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.height - 2, cellRect.height - 2);
                        GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit);
                        DefaultGUI.Label(new Rect(cellRect.x + iconRect.xMax + 1, cellRect.y, cellRect.width - iconRect.width, cellRect.height), item.displayName, args.selected, args.focused);
                    }
                    break;
                case 1:
                    DefaultGUI.Label(cellRect, item.asset.m_bundle == null ? string.Empty : item.asset.m_bundle.m_name, args.selected, args.focused);
                    break;
                case 2:
                    DefaultGUI.Label(cellRect, item.asset.GetSizeString(), args.selected, args.focused);
                    break;
            }
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

        internal void SetSelectedBundles(IEnumerable<AssetBundleState.BundleInfo> b)
        {
            m_selectionList.SetItems(null);
            m_data = new DisplayData(b);
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
            return GetRows().Where(a => ids.Contains(a.id)).Select(a => (a as AssetBundleState.AssetInfo.TreeItem).asset);
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
                AssetBundleState.StartABMoveBatch();
                foreach (var b in m_data.m_bundles)
                    AssetBundleState.MoveAssetsToBundle(DragAndDrop.paths.Select(a => AssetBundleState.GetAsset(a)), b.m_name);
                AssetBundleState.EndABMoveBatch();
                SetSelectedBundles(m_data.m_bundles);
            }

            return DragAndDropVisualMode.Move;
        }

        protected override void KeyEvent()
        {
            if (m_data != null && Event.current.keyCode == KeyCode.Delete && GetSelection().Count > 0)
            {
                AssetBundleState.StartABMoveBatch();
                AssetBundleState.MoveAssetsToBundle(GetSelectedAssets(), string.Empty);
                AssetBundleState.EndABMoveBatch();
                SetSelectedBundles(m_data.m_bundles);
                Event.current.Use();
            }
        }
    }
}
