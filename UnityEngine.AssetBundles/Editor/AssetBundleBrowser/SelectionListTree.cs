using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using System;


namespace UnityEngine.AssetBundles
{
    internal class SelectionListTree : TreeView
    {
        public SelectionListTree(TreeViewState state) : base(state)
        {
            showBorder = true;
          //  Reload();
        }
        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            root.children = new List<TreeViewItem>();

            if (m_selecteditems != null)
            {
				int index = 0;
				foreach (var a in m_selecteditems)
				{
					var item = new TreeViewItem(a.name.GetHashCode(), 0, root, a.name);
					item.userData = a;
					item.icon = AssetDatabase.GetCachedIcon(a.name) as Texture2D;
					root.AddChild(item);
					var deps = AssetDatabase.GetDependencies(a.name, true);
					if (deps.Length > 1)
					{
						var refItem = new TreeViewItem(index, 1, (deps.Length - 1) + " dependenc" + (deps.Length == 2 ? "y" : "ies"));
						refItem.icon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName) as Texture2D;
						item.AddChild(refItem);
						foreach (var d in deps)
						{
							if (d != a.name)
							{
								var di = new TreeViewItem(d.GetHashCode(), 2, d);
								di.icon = AssetDatabase.GetCachedIcon(d) as Texture2D;
								di.userData = AssetBundleState.assets[d];
								refItem.AddChild(di);
							}
						}
						index++;
					}
				}
			}
            return root;
        }

        protected override void DoubleClickedItem(int id)
        {
            var assetInfo = TreeViewUtility.FindItem(id, rootItem).userData as AssetBundleState.AssetInfo;
            if (assetInfo != null)
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(assetInfo.name);
        }

        internal void Clear()
        {
            m_selecteditems = null;
            Reload();
        }
        IEnumerable<AssetBundleState.AssetInfo> m_selecteditems;
        internal void SetItems(IEnumerable<AssetBundleState.AssetInfo> items)
        {
            if (HasSelection())
                SetSelection(new List<int>());
            m_selecteditems = items;
            Reload();
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            args.draggedItemIDs = GetSelection();
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.paths = GetRowsFromIDs(args.draggedItemIDs).Select(a => a.userData == null ? string.Empty : (a.userData as AssetBundleState.AssetInfo).name).ToArray();
            DragAndDrop.StartDrag("SelectionListTree");
        }

    }
}
