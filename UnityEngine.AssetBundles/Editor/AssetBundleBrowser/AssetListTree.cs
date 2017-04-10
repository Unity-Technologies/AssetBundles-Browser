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
        List<AssetBundleModel.BundleInfo> m_sourceBundles = new List<AssetBundleModel.BundleInfo>();
        AssetBundleManageTab m_controller;

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            return new MultiColumnHeaderState(GetColumns());
        }
        private static MultiColumnHeaderState.Column[] GetColumns()
        {
            var retVal = new MultiColumnHeaderState.Column[] {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column()
            };
            retVal[0].headerContent = new GUIContent("Asset", "Short name of asset. For full name select asset and see message below");
            retVal[0].minWidth = 50;
            retVal[0].width = 100;
            retVal[0].maxWidth = 300;
            retVal[0].headerTextAlignment = TextAlignment.Left;
            retVal[0].canSort = true;
            retVal[0].autoResize = true;

            retVal[1].headerContent = new GUIContent("Bundle", "Bundle name. 'auto' means asset was pulled in due to dependency");
            retVal[1].minWidth = 50;
            retVal[1].width = 100;
            retVal[1].maxWidth = 300;
            retVal[1].headerTextAlignment = TextAlignment.Left;
            retVal[1].canSort = true;
            retVal[1].autoResize = true;

            retVal[2].headerContent = new GUIContent("Size", "Size on disk");
            retVal[2].minWidth = 30;
            retVal[2].width = 75;
            retVal[2].maxWidth = 100;
            retVal[2].headerTextAlignment = TextAlignment.Left;
            retVal[2].canSort = true;
            retVal[2].autoResize = true;

            retVal[3].headerContent = new GUIContent("!", "Errors, Warnings, or Info");
            retVal[3].minWidth = 16;
            retVal[3].width = 16;
            retVal[3].maxWidth = 16;
            retVal[3].headerTextAlignment = TextAlignment.Left;
            retVal[3].canSort = true;
            retVal[3].autoResize = false;

            return retVal;
        }
        enum MyColumns
        {
            Asset,
            Bundle,
            Size,
            Message
        }
        public enum SortOption
        {
            Asset,
            Bundle,
            Size,
            Message
        }
        SortOption[] m_SortOptions =
        {
            SortOption.Asset,
            SortOption.Bundle,
            SortOption.Size,
            SortOption.Message
        };

        public AssetListTree(TreeViewState state, MultiColumnHeaderState mchs, AssetBundleManageTab ctrl ) : base(state, new MultiColumnHeader(mchs))
        {
            m_controller = ctrl;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            DefaultStyles.label.richText = true;
            multiColumnHeader.sortingChanged += OnSortingChanged;
        }


        public void Update()
        {
            bool dirty = false;
            foreach (var bundle in m_sourceBundles)
            {
                dirty |= bundle.Dirty;
            }
            if (dirty)
                Reload();
        }
        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }


        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            SortIfNeeded(root, rows);
            return rows;
        }

        internal void SetSelectedBundles(IEnumerable<AssetBundleModel.BundleInfo> bundles)
        {
            m_controller.SetSelectedItems(null);
            m_sourceBundles = bundles.ToList();
            SetSelection(new List<int>());
            Reload();
        }
        protected override TreeViewItem BuildRoot()
        {
            var root = AssetBundleModel.Model.CreateAssetListTreeView(m_sourceBundles);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            Color oldColor = GUI.color;
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), args.item as AssetBundleModel.AssetTreeItem, args.GetColumn(i), ref args);
            GUI.color = oldColor;
        }

        private void CellGUI(Rect cellRect, AssetBundleModel.AssetTreeItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            GUI.color = item.ItemColor;
            switch (column)
            {
                case 0:
                    {
                        var iconRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.height - 2, cellRect.height - 2);
                        GUI.DrawTexture(iconRect, item.icon, ScaleMode.ScaleToFit);
                        DefaultGUI.Label(
                            new Rect(cellRect.x + iconRect.xMax + 1, cellRect.y, cellRect.width - iconRect.width, cellRect.height), 
                            item.displayName, 
                            args.selected, 
                            args.focused);
                    }
                    break;
                case 1:
                    DefaultGUI.Label(cellRect, item.asset.BundleName, args.selected, args.focused);
                    break;
                case 2:
                    DefaultGUI.Label(cellRect, item.asset.GetSizeString(), args.selected, args.focused);
                    break;
                case 3:
                    var icon = AssetBundleModel.ProblemMessage.GetIcon(item.HighestMessageLevel());
                    if (icon != null)
                    {
                        //var iconRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.height - 2, cellRect.height - 2);
                        var iconRect = new Rect(cellRect.x, cellRect.y, cellRect.height, cellRect.height);
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                    }
                    break;
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var assetItem = FindItem(id, rootItem) as AssetBundleModel.AssetTreeItem;
			if (assetItem != null)
			{
				Object o = AssetDatabase.LoadAssetAtPath<Object>(assetItem.asset.Name);
				EditorGUIUtility.PingObject(o);
				Selection.activeObject = o;
			}
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            List<AssetBundleModel.AssetInfo> selectedAssets = new List<AssetBundleModel.AssetInfo>();
            foreach (var id in selectedIds)
            {
                var assetItem = FindItem(id, rootItem) as AssetBundleModel.AssetTreeItem;
                if (assetItem != null)
                {
                    Object o = AssetDatabase.LoadAssetAtPath<Object>(assetItem.asset.Name);
                    Selection.activeObject = o;
                    selectedAssets.Add(assetItem.asset);
                }
            }
            m_controller.SetSelectedItems(selectedAssets);
        }
        
        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            args.draggedItemIDs = GetSelection();
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            List<AssetBundleModel.AssetTreeItem> items = 
                new List<AssetBundleModel.AssetTreeItem>(args.draggedItemIDs.Select(id => FindItem(id, rootItem) as AssetBundleModel.AssetTreeItem));
            DragAndDrop.paths = items.Select(a => a.asset.Name).ToArray();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { };
            DragAndDrop.SetGenericData("AssetListTreeSource", this);
            DragAndDrop.StartDrag("AssetListTree");
        }
        
        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if(IsValidDragDrop(args))
            {
                if (args.performDrop)
                {
                    AssetBundleModel.Model.MoveAssetToBundle(DragAndDrop.paths, m_sourceBundles[0].m_name.BundleName, m_sourceBundles[0].m_name.Variant);
                    AssetBundleModel.Model.ExecuteAssetMove();
                    foreach (var bundle in m_sourceBundles)
                    {
                        bundle.RefreshAssetList();
                    }
                    m_controller.UpdateSelectedBundles(m_sourceBundles);
                }
                return DragAndDropVisualMode.Copy;//Move;
            }

            return DragAndDropVisualMode.Rejected;
        }
        protected bool IsValidDragDrop(DragAndDropArgs args)
        {
            //can't drag onto none or >1 bundles
            if (m_sourceBundles.Count == 0 || m_sourceBundles.Count > 1)
                return false;
            
            //can't drag nothing
            if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0)
                return false;

            //can't drag into a folder
            var folder = m_sourceBundles[0] as AssetBundleModel.BundleFolderInfo;
            if (folder != null)
                return false;

            var data = m_sourceBundles[0] as AssetBundleModel.BundleDataInfo;
            if(data == null)
                return false; // this should never happen.

            var thing = DragAndDrop.GetGenericData("AssetListTreeSource") as AssetListTree;
            if (thing != null)
                return false;
            
            if(data.IsEmpty())
                return true;

            if (data.IsSceneBundle)
                return false;


            foreach (var assetPath in DragAndDrop.paths)
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(SceneAsset))
                    return false;
            }

            return true;

        }

        protected override void ContextClickedItem(int id)
        {
            List<AssetBundleModel.AssetTreeItem> selectedNodes = new List<AssetBundleModel.AssetTreeItem>();
            foreach(var nodeID in GetSelection())
            {
                selectedNodes.Add(FindItem(nodeID, rootItem) as AssetBundleModel.AssetTreeItem);
            }

            if(selectedNodes.Count > 0)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Remove asset(s) from bundle."), false, RemoveAssets, selectedNodes);
                menu.ShowAsContext();
            }

        }
        void RemoveAssets(object obj)
        {
            var selectedNodes = obj as List<AssetBundleModel.AssetTreeItem>;
            var assets = new List<AssetBundleModel.AssetInfo>();
            //var bundles = new List<AssetBundleModel.BundleInfo>();
            foreach (var node in selectedNodes)
            {
                if (node.asset.BundleName != string.Empty)
                    assets.Add(node.asset);
            }
            AssetBundleModel.Model.MoveAssetToBundle(assets, string.Empty, string.Empty);
            AssetBundleModel.Model.ExecuteAssetMove();
            foreach (var bundle in m_sourceBundles)
            {
                bundle.RefreshAssetList();
            }
            m_controller.UpdateSelectedBundles(m_sourceBundles);
            //ReloadAndSelect(new List<int>());
        }

        protected override void KeyEvent()
        {
            if (m_sourceBundles.Count > 0 && Event.current.keyCode == KeyCode.Delete && GetSelection().Count > 0)
            {
                List<AssetBundleModel.AssetTreeItem> selectedNodes = new List<AssetBundleModel.AssetTreeItem>();
                foreach (var nodeID in GetSelection())
                {
                    selectedNodes.Add(FindItem(nodeID, rootItem) as AssetBundleModel.AssetTreeItem);
                }

                RemoveAssets(selectedNodes);
            }
        }
        void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SortIfNeeded(rootItem, GetRows());
        }
        void SortIfNeeded(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
                return;

            if (multiColumnHeader.sortedColumnIndex == -1)
                return;

            SortByColumn();

            rows.Clear();
            for (int i = 0; i < root.children.Count; i++)
                rows.Add(root.children[i]);

            Repaint();
        }
        void SortByColumn()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if (sortedColumns.Length == 0)
                return;

            List<AssetBundleModel.AssetTreeItem> assetList = new List<AssetBundleModel.AssetTreeItem>();
            foreach(var item in rootItem.children)
            {
                assetList.Add(item as AssetBundleModel.AssetTreeItem);
            }
            var orderedItems = InitialOrder(assetList, sortedColumns);

            rootItem.children = orderedItems.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<AssetBundleModel.AssetTreeItem> InitialOrder(IEnumerable<AssetBundleModel.AssetTreeItem> myTypes, int[] columnList)
        {
            SortOption sortOption = m_SortOptions[columnList[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(columnList[0]);
            switch (sortOption)
            {
                case SortOption.Asset:
                    return myTypes.Order(l => l.displayName, ascending);
                case SortOption.Size:
                    return myTypes.Order(l => l.asset.fileSize, ascending);
                case SortOption.Message:
                    return myTypes.Order(l => l.HighestMessageLevel(), ascending);
                case SortOption.Bundle:
                default:
                    return myTypes.Order(l => l.asset.BundleName, ascending);
            }
            
        }

        private void ReloadAndSelect(IList<int> hashCodes)
        {
            Reload();
            SetSelection(hashCodes);
            SelectionChanged(hashCodes);
        }
    }
    static class MyExtensionMethods
    {
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }
            else
            {
                return source.OrderByDescending(selector);
            }
        }

        public static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.ThenBy(selector);
            }
            else
            {
                return source.ThenByDescending(selector);
            }
        }
    }
}
