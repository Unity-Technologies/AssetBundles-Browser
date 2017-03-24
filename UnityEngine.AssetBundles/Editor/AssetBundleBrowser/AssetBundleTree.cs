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
        AssetBundleManageTab m_controller;
       
        public AssetBundleTree(TreeViewState state, AssetBundleManageTab ctrl) : base(state)
        {
            AssetBundleModel.Model.Rebuild();
            m_controller = ctrl;
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

        protected override void RowGUI(RowGUIArgs args)
        {
            if (args.item.icon == null)
                extraSpaceBeforeIconAndLabel = 16f;
            else
                extraSpaceBeforeIconAndLabel = 0f;

            base.RowGUI(args);

            var errorIcon = (args.item as AssetBundleModel.BundleTreeItem).GetErrorIcon();
            if (errorIcon != null)
            {
                var size = args.rowRect.height;
                var right = args.rowRect.xMax;
                Rect messageRect = new Rect(right - size, args.rowRect.yMin, size, size);
                GUI.Label(messageRect, new GUIContent(errorIcon, (args.item as AssetBundleModel.BundleTreeItem).ErrorMessage() ));
            }
        }

        protected override void RenameEnded(RenameEndedArgs args)
        { 
            base.RenameEnded(args);
            if (args.newName.Length > 0 && args.newName != args.originalName)
            {
                args.newName = args.newName.ToLower();
                args.acceptedRename = true;

                AssetBundleModel.BundleTreeItem renamedItem = FindItem(args.itemID, rootItem) as AssetBundleModel.BundleTreeItem;
                args.acceptedRename = AssetBundleModel.Model.HandleBundleRename(renamedItem, args.newName);
                ReloadAndSelect(renamedItem.bundle.NameHashCode, false);
            }
            else
            {
                args.acceptedRename = false;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            AssetBundleModel.Model.Refresh();
            var root = AssetBundleModel.Model.CreateBundleTreeView();
            return root;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            var selectedBundles = new List<AssetBundleModel.BundleInfo>();
            foreach (var id in selectedIds)
            {
                var item = FindItem(id, rootItem) as AssetBundleModel.BundleTreeItem;
                item.bundle.RefreshAssetList();
                selectedBundles.Add(item.bundle);
            }

            m_controller.UpdateSelectedBundles(selectedBundles);
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);
            if(Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }


        //if I could set base.m_TreeView.deselectOnUnhandledMouseDown then I wouldn't need m_contextOnItem...
        private bool m_contextOnItem = false;
        protected override void ContextClicked()
        {
            if (m_contextOnItem)
            {
                m_contextOnItem = false;
                return;
            }

            List<AssetBundleModel.BundleTreeItem> selectedNodes = new List<AssetBundleModel.BundleTreeItem>();
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add new bundle"), false, CreateNewBundle, selectedNodes); 
            menu.AddItem(new GUIContent("Add new folder"), false, CreateFolder, selectedNodes);
            menu.ShowAsContext();
        }

        protected override void ContextClickedItem(int id)
        {
            m_contextOnItem = true;
            List<AssetBundleModel.BundleTreeItem> selectedNodes = new List<AssetBundleModel.BundleTreeItem>();
            foreach (var nodeID in GetSelection())
            {
                selectedNodes.Add(FindItem(nodeID, rootItem) as AssetBundleModel.BundleTreeItem);
            }
            
            GenericMenu menu = new GenericMenu();

            if(selectedNodes.Count == 1)
            {
                var folder = selectedNodes[0].bundle as AssetBundleModel.BundleFolderInfo;
                if(folder != null)
                {
                    menu.AddItem(new GUIContent("Add new bundle"), false, CreateNewBundle, selectedNodes);
                    menu.AddItem(new GUIContent("Add new folder"), false, CreateFolder, selectedNodes); 
                }
                menu.AddItem(new GUIContent("Delete " + selectedNodes[0].displayName), false, DeleteBundles, selectedNodes);

            }
            else if (selectedNodes.Count > 1)
            {
                menu.AddItem(new GUIContent("Move duplicated assets to new bundle"), false, DedupeBundles, selectedNodes);
                menu.AddItem(new GUIContent("Delete multiple bundles"), false, DeleteBundles, selectedNodes);
            }
            menu.ShowAsContext();
        }

        void CreateFolder(object context)
        {
            AssetBundleModel.BundleFolderInfo folder = null;
            var selectedNodes = context as List<AssetBundleModel.BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0)
            {
                folder = selectedNodes[0].bundle as AssetBundleModel.BundleFolderInfo;
            }
            var newBundle = AssetBundleModel.Model.CreateEmptyBundleFolder(folder);
            ReloadAndSelect(newBundle.NameHashCode, true);
        }
        void CreateNewBundle(object context)
        {
            AssetBundleModel.BundleFolderInfo folder = null;
            var selectedNodes = context as List<AssetBundleModel.BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0)
            {
                folder = selectedNodes[0].bundle as AssetBundleModel.BundleFolderInfo;
            }
            var newBundle = AssetBundleModel.Model.CreateEmptyBundle(folder);
            ReloadAndSelect(newBundle.NameHashCode, true);
        }
        void DedupeBundles(object context)
        {
            var selectedNodes = context as List<AssetBundleModel.BundleTreeItem>;
            var newBundle = AssetBundleModel.Model.HandleDedupeBundles(selectedNodes.Select(item => item.bundle));
            var selection = new List<int>();
            selection.Add(newBundle.NameHashCode);
            ReloadAndSelect(selection);
        }

        void DeleteBundles(object b)
        {
            var selectedNodes = b as List<AssetBundleModel.BundleTreeItem>;
            AssetBundleModel.Model.HandleBundleDelete(selectedNodes.Select(item => item.bundle));
            ReloadAndSelect(new List<int>());
        }
        protected override void KeyEvent()
        {
            if (Event.current.keyCode == KeyCode.Delete && GetSelection().Count > 0)
            {
                List<AssetBundleModel.BundleTreeItem> selectedNodes = new List<AssetBundleModel.BundleTreeItem>();
                foreach (var nodeID in GetSelection())
                {
                    selectedNodes.Add(FindItem(nodeID, rootItem) as AssetBundleModel.BundleTreeItem);
                }
                DeleteBundles(selectedNodes);
            }
        }

        class DragAndDropData
        {
            public bool hasBundleFolder = false;
            public bool hasScene = false;
            public bool hasNonScene = false;
            public List<AssetBundleModel.BundleInfo> draggedNodes;
            public AssetBundleModel.BundleTreeItem targetNode;
            public DragAndDropArgs args;
            public string[] paths;

            public DragAndDropData(DragAndDropArgs a)
            {
                args = a;
                draggedNodes = DragAndDrop.GetGenericData("AssetBundleModel.BundleInfo") as List<AssetBundleModel.BundleInfo>;
                targetNode = args.parentItem as AssetBundleModel.BundleTreeItem;
                paths = DragAndDrop.paths;

                if (draggedNodes != null)
                {
                    foreach (var bundle in draggedNodes)
                    {
                        if ((bundle as AssetBundleModel.BundleFolderInfo) != null)
                        {
                            hasBundleFolder = true;
                        }
                        else
                        {
                            var dataBundle = bundle as AssetBundleModel.BundleDataInfo;
                            if (dataBundle != null)
                            {
                                if (dataBundle.IsSceneBundle)
                                    hasScene = true;
                                else
                                    hasNonScene = true;
                            }
                        }
                    }
                }
                else if (DragAndDrop.paths != null)
                {
                    foreach (var assetPath in DragAndDrop.paths)
                    {
                        if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(SceneAsset))
                            hasScene = true;
                        else
                            hasNonScene = true;
                    }
                }
            }

        }
        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.None;
            DragAndDropData data = new DragAndDropData(args);
            
            if (data.hasScene && data.hasNonScene)
                return DragAndDropVisualMode.Rejected;
            
            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.UponItem:
                    visualMode = HandleDragDropUpon(data);
                    break;
                case DragAndDropPosition.BetweenItems:
                    visualMode = HandleDragDropBetween(data);
                    break;
                case DragAndDropPosition.OutsideItems:
                    if(data.draggedNodes != null)
                    {
                        visualMode = DragAndDropVisualMode.Rejected;
                    }
                    else if(data.paths != null)
                    {
                        visualMode = DragAndDropVisualMode.Generic;

                        if(data.args.performDrop)
                        {
                            DragPathsToNewSpace(data.paths, null, data.hasScene);
                        }
                    }
                    break;
            }
            return visualMode;
        }

        private DragAndDropVisualMode HandleDragDropUpon(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Move;
            var targetDataBundle = data.targetNode.bundle as AssetBundleModel.BundleDataInfo;
            if (targetDataBundle != null)
            {
                if (targetDataBundle.IsSceneBundle)
                    visualMode = DragAndDropVisualMode.Rejected;
                else
                {
                    if (data.hasScene || data.hasBundleFolder)
                    {
                        return DragAndDropVisualMode.Rejected;
                    }
                    else
                    {
                        if (data.args.performDrop)
                        {
                            if (data.draggedNodes != null)
                            {
                                AssetBundleModel.Model.HandleBundleMerge(data.draggedNodes, targetDataBundle);
                                ReloadAndSelect(targetDataBundle.NameHashCode, false);
                            }
                            else if (data.paths != null)
                            {
                                AssetBundleModel.Model.MoveAssetToBundle(data.paths, targetDataBundle.m_name.Name);
                                AssetBundleModel.Model.ExecuteAssetMove();
                                ReloadAndSelect(targetDataBundle.NameHashCode, false);
                            }
                        }
                    }
                }
            }
            else
            {
                var folder = data.targetNode.bundle as AssetBundleModel.BundleFolderInfo;
                if (folder != null && data.args.performDrop)
                {
                    if (data.draggedNodes != null)
                    {
                        AssetBundleModel.Model.HandleBundleReparent(data.draggedNodes, folder);
                        Reload();
                    }
                    else if (data.paths != null)
                    {
                        DragPathsToNewSpace(data.paths, folder, data.hasScene);
                    }
                }
            }
            return visualMode;
        }
        private DragAndDropVisualMode HandleDragDropBetween(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Move;

            if (data.args.performDrop)
            {
                var parent = (data.args.parentItem as AssetBundleModel.BundleTreeItem);
                if (parent != null)
                {
                    var folder = parent.bundle as AssetBundleModel.BundleFolderInfo;
                    if (folder != null)
                    {
                        if (data.draggedNodes != null)
                        {
                            AssetBundleModel.Model.HandleBundleReparent(data.draggedNodes, folder);
                            Reload();
                        }
                        else if (data.paths != null)
                        {
                            DragPathsToNewSpace(data.paths, folder, data.hasScene);
                        }
                    }
                }
            }

            return visualMode;
        }

        private void DragPathsToNewSpace(string[] paths, AssetBundleModel.BundleFolderInfo root, bool hasScene)
        {
            if (hasScene)
            {
                List<int> hashCodes = new List<int>();
                foreach (var assetPath in paths)
                {
                    var newBundle = AssetBundleModel.Model.CreateEmptyBundle(root, System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLower());
                    AssetBundleModel.Model.MoveAssetToBundle(assetPath, newBundle.m_name.Name);
                    hashCodes.Add(newBundle.NameHashCode);
                }
                AssetBundleModel.Model.ExecuteAssetMove();
                ReloadAndSelect(hashCodes);
            }
            else
            {
                var newBundle = AssetBundleModel.Model.CreateEmptyBundle(root);
                AssetBundleModel.Model.MoveAssetToBundle(paths, newBundle.m_name.Name);
                AssetBundleModel.Model.ExecuteAssetMove();
                ReloadAndSelect(newBundle.NameHashCode, true);
            }
        }
        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            var selectedBundles = new List<AssetBundleModel.BundleInfo>();
            foreach (var id in args.draggedItemIDs)
            {
                var item = FindItem(id, rootItem) as AssetBundleModel.BundleTreeItem;
                selectedBundles.Add(item.bundle);
            }
            DragAndDrop.paths = null;
            DragAndDrop.objectReferences = new UnityEngine.Object[] { };
            DragAndDrop.SetGenericData("AssetBundleModel.BundleInfo", selectedBundles);
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            DragAndDrop.StartDrag("AssetBundleTree");
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            //args.draggedItemIDs = GetSelection();
            return true;
        }

        internal void Refresh()
        {
            var selection = GetSelection();
            Reload();
            SelectionChanged(selection);
        }

        private void ReloadAndSelect(int hashCode, bool rename)
        {
            var selection = new List<int>();
            selection.Add(hashCode);
            ReloadAndSelect(selection);
            if(rename)
            {
                BeginRename(FindItem(hashCode, rootItem), 0.25f);
            }
        }
        private void ReloadAndSelect(IList<int> hashCodes)
        {
            Reload();
            SetSelection(hashCodes);
            SelectionChanged(hashCodes);
        }
    }
}
