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
            var bundleItem = (args.item as AssetBundleModel.BundleTreeItem);
            if (args.item.icon == null)
                extraSpaceBeforeIconAndLabel = 16f;
            else
                extraSpaceBeforeIconAndLabel = 0f;

            Color old = GUI.color;
            if ((bundleItem.bundle as AssetBundleModel.BundleVariantFolderInfo) != null)
                GUI.color = AssetBundleModel.Model.kLightGrey; //new Color(0.3f, 0.5f, 0.85f);
            base.RowGUI(args);
            GUI.color = old;

            var errorIcon = bundleItem.GetErrorIcon();
            if (errorIcon != null)
            {
                var size = args.rowRect.height;
                var right = args.rowRect.xMax;
                Rect messageRect = new Rect(right - size, args.rowRect.yMin, size, size);
                GUI.Label(messageRect, new GUIContent(errorIcon, bundleItem.ErrorMessage() ));
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
            menu.AddItem(new GUIContent("Reload all data"), false, ForceReloadData, selectedNodes);
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
                if ((selectedNodes[0].bundle as AssetBundleModel.BundleFolderConcreteInfo) != null)
                {
                    menu.AddItem(new GUIContent("Add new bundle"), false, CreateNewBundle, selectedNodes);
                    menu.AddItem(new GUIContent("Add new folder"), false, CreateFolder, selectedNodes);
                }
                else if( (selectedNodes[0].bundle as AssetBundleModel.BundleVariantFolderInfo) != null)
                {
                    menu.AddItem(new GUIContent("Add new variant"), false, CreateNewVariant, selectedNodes);
                }
                else
                {
                    var variant = selectedNodes[0].bundle as AssetBundleModel.BundleVariantDataInfo;
                    if(variant == null)
                       menu.AddItem(new GUIContent("Convert to variant"), false, ConvertToVariant, selectedNodes);
                }
                if(selectedNodes[0].bundle.HasWarning())
                    menu.AddItem(new GUIContent("Move duplicates to new bundle"), false, DedupeAllBundles, selectedNodes);
                menu.AddItem(new GUIContent("Rename"), false, RenameBundle, selectedNodes);
                menu.AddItem(new GUIContent("Delete " + selectedNodes[0].displayName), false, DeleteBundles, selectedNodes);
                
            }
            else if (selectedNodes.Count > 1)
            {
                menu.AddItem(new GUIContent("Move duplicates shared by selected"), false, DedupeOverlappedBundles, selectedNodes);
                menu.AddItem(new GUIContent("Move duplicates existing in any selected"), false, DedupeAllBundles, selectedNodes);
                menu.AddItem(new GUIContent("Delete multiple bundles"), false, DeleteBundles, selectedNodes);
            }
            menu.ShowAsContext();
        }
        void ForceReloadData(object context)
        {
            AssetBundleModel.Model.ForceReloadData(this);
        }
        void CreateFolder(object context)
        {
            AssetBundleModel.BundleFolderConcreteInfo folder = null;
            var selectedNodes = context as List<AssetBundleModel.BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0)
            {
                folder = selectedNodes[0].bundle as AssetBundleModel.BundleFolderConcreteInfo;
            }
            var newBundle = AssetBundleModel.Model.CreateEmptyBundleFolder(folder);
            ReloadAndSelect(newBundle.NameHashCode, true);
        }
        void RenameBundle(object context)
        {
            var selectedNodes = context as List<AssetBundleModel.BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0)
            {
                BeginRename(FindItem(selectedNodes[0].bundle.NameHashCode, rootItem), 0.1f);
            }
        }

        void CreateNewBundle(object context)
        {
            AssetBundleModel.BundleFolderConcreteInfo folder = null;
            var selectedNodes = context as List<AssetBundleModel.BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count > 0)
            {
                folder = selectedNodes[0].bundle as AssetBundleModel.BundleFolderConcreteInfo;
            }
            var newBundle = AssetBundleModel.Model.CreateEmptyBundle(folder);
            ReloadAndSelect(newBundle.NameHashCode, true);
        }

        void CreateNewVariant(object context)
        {
            AssetBundleModel.BundleVariantFolderInfo folder = null;
            var selectedNodes = context as List<AssetBundleModel.BundleTreeItem>;
            if (selectedNodes != null && selectedNodes.Count == 1)
            {
                folder = selectedNodes[0].bundle as AssetBundleModel.BundleVariantFolderInfo;
                if (folder != null)
                {
                    var newBundle = AssetBundleModel.Model.CreateEmptyVariant(folder);
                    ReloadAndSelect(newBundle.NameHashCode, true);
                }
            }
        }

        void ConvertToVariant(object context)
        {
            var selectedNodes = context as List<AssetBundleModel.BundleTreeItem>;
            if (selectedNodes.Count == 1)
            {
                var bundle = selectedNodes[0].bundle as AssetBundleModel.BundleDataInfo;
                var newBundle = AssetBundleModel.Model.ConvertToVariant(bundle);
                int hash = 0;
                if (newBundle != null)
                    hash = newBundle.NameHashCode;
                ReloadAndSelect(hash, true);
            }
        }

        void DedupeOverlappedBundles(object context)
        {
            DedupeBundles(context, true);
        }
        void DedupeAllBundles(object context)
        {
            DedupeBundles(context, false);
        }
        void DedupeBundles(object context, bool onlyOverlappedAssets)
        {
            var selectedNodes = context as List<AssetBundleModel.BundleTreeItem>;
            var newBundle = AssetBundleModel.Model.HandleDedupeBundles(selectedNodes.Select(item => item.bundle), onlyOverlappedAssets);
            if(newBundle != null)
            {
                var selection = new List<int>();
                selection.Add(newBundle.NameHashCode);
                ReloadAndSelect(selection);
            }
            else
            {
                if (onlyOverlappedAssets)
                    Debug.LogWarning("There were no duplicated assets that existed across all selected bundles.");
                else
                    Debug.LogWarning("No duplicate assets found after refreshing bundle contents.");
            }
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
            public bool hasVariantChild = false;
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

                                if ( (dataBundle as AssetBundleModel.BundleVariantDataInfo) != null)
                                    hasVariantChild = true;
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
            
            if ( (data.hasScene && data.hasNonScene) ||
                (data.hasVariantChild) )
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
                    if (data.draggedNodes != null)
                    {
                        visualMode = DragAndDropVisualMode.Copy;// Generic;
                        if (data.args.performDrop)
                        {
                            AssetBundleModel.Model.HandleBundleReparent(data.draggedNodes, null);
                            Reload();
                        }
                    }
                    else if(data.paths != null)
                    {
                        visualMode = DragAndDropVisualMode.Copy;//Generic;
                        if (data.args.performDrop)
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
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Copy;//Move;
            var targetDataBundle = data.targetNode.bundle as AssetBundleModel.BundleDataInfo;
            if (targetDataBundle != null)
            {
                if (targetDataBundle.IsSceneBundle)
                    visualMode = DragAndDropVisualMode.Rejected;
                else
                {
                    if( (data.hasBundleFolder) || (data.hasScene && !targetDataBundle.IsEmpty()))
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
                                AssetBundleModel.Model.MoveAssetToBundle(data.paths, targetDataBundle.m_name.BundleName, targetDataBundle.m_name.Variant);
                                AssetBundleModel.Model.ExecuteAssetMove();
                                ReloadAndSelect(targetDataBundle.NameHashCode, false);
                            }
                        }
                    }
                }
            }
            else
            {
                var folder = data.targetNode.bundle as AssetBundleModel.BundleFolderConcreteInfo;
                if (folder != null)
                {
                    if(data.args.performDrop)
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
                else
                    visualMode = DragAndDropVisualMode.Rejected; //must be a variantfolder
            }
            return visualMode;
        }
        private DragAndDropVisualMode HandleDragDropBetween(DragAndDropData data)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.Copy;//Move;

            var parent = (data.args.parentItem as AssetBundleModel.BundleTreeItem);

            if (parent != null)
            {
                var variantFolder = parent.bundle as AssetBundleModel.BundleVariantFolderInfo;
                if (variantFolder != null)
                    return DragAndDropVisualMode.Rejected;

                if (data.args.performDrop)
                {
                    var folder = parent.bundle as AssetBundleModel.BundleFolderConcreteInfo;
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
                    AssetBundleModel.Model.MoveAssetToBundle(assetPath, newBundle.m_name.BundleName, string.Empty);
                    hashCodes.Add(newBundle.NameHashCode);
                }
                AssetBundleModel.Model.ExecuteAssetMove();
                ReloadAndSelect(hashCodes);
            }
            else
            {
                var newBundle = AssetBundleModel.Model.CreateEmptyBundle(root);
                AssetBundleModel.Model.MoveAssetToBundle(paths, newBundle.m_name.BundleName, string.Empty);
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
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;//Move;
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
            SetSelection(hashCodes, TreeViewSelectionOptions.RevealAndFrame);
            SelectionChanged(hashCodes);
        }
    }
}
