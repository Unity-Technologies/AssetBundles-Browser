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
        public AssetBundleTree m_bundleTree;
      //  public AssetListTree m_listTree;
        public SelectionListTree(TreeViewState state) : base(state)
        {
            showBorder = true;
        }
        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            root.children = new List<TreeViewItem>();

            if (m_selecteditems != null)
            {
                foreach (var a in m_selecteditems)
                {
                    int index = 0;
                    var item = new TreeViewItem(a.name.GetHashCode(), 0, root, a.name);
                    item.userData = a;
                    item.icon = AssetDatabase.GetCachedIcon(a.name) as Texture2D;
                    root.AddChild(item);
                    var refs = new List<AssetBundleState.AssetInfo>();
                    a.GatherReferences(refs);
                    if (refs.Count > 0)
                    {
                        var refItem = new TreeViewItem(index++, 1, refs.Count + " reference" + (refs.Count == 1 ? "" : "s"));
                        refItem.icon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName) as Texture2D;
                        item.AddChild(refItem);

                        foreach (var d in refs)
                        {
                            var di = new TreeViewItem(d.name.GetHashCode(), 2, d.name);
                            di.icon = AssetDatabase.GetCachedIcon(d.name) as Texture2D;
                            di.userData = d;
                            refItem.AddChild(di);
                        }
                    }

                    var bundles = new List<AssetBundleState.BundleInfo>();
                    a.GatherBundles(bundles);
                    if (bundles.Count > 0)
                    {
                        var refItem = new TreeViewItem(index++, 1, bundles.Count + " bundle" + (bundles.Count == 1 ? "" : "s"));
                        refItem.icon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName) as Texture2D;
                        item.AddChild(refItem);

                        foreach (var d in bundles)
                        {
                            var di = new TreeViewItem(d.name.GetHashCode(), 2, d.name);
                            di.icon = AssetDatabase.GetCachedIcon(d.name) as Texture2D;
                            di.userData = d;
                            refItem.AddChild(di);
                        }
                    }

                }
            }
            return root;
        }

        /*
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
                    var deps = new HashSet<string>();
                    GatherDependencies(a, deps);
					if (deps.Count > 0)
					{
						var refItem = new TreeViewItem(index, 1, deps.Count + " dependenc" + (deps.Count == 1 ? "y" : "ies"));
						refItem.icon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName) as Texture2D;
						item.AddChild(refItem);
						foreach (var d in deps)
						{
							if (d != a.name)
							{
								var di = new TreeViewItem(d.GetHashCode(), 2, d);
								di.icon = AssetDatabase.GetCachedIcon(d) as Texture2D;
								di.userData = AssetBundleState.GetAsset(d);
								refItem.AddChild(di);
							}
						}
						index++;
					}
				}
			}
            return root;
        }
        /*
        void GatherDependencies(AssetBundleState.AssetInfo a, HashSet<string> deps)
        {
            if (a == null)
                return;

            string currentBundle = AssetDatabase.GetImplicitAssetBundleName(a.name);

            foreach (var d in AssetDatabase.GetDependencies(a.name, true))
            {
                if (d != a.name)
                {
                    var b = AssetDatabase.GetImplicitAssetBundleName(d);
                    if(string.IsNullOrEmpty(b) || b == currentBundle)
                        deps.Add(d);
                }
            }

            if (AssetDatabase.IsValidFolder(a.name))
            {
                foreach (var f in System.IO.Directory.GetFiles(a.name))
                {
                    var ai = AssetBundleState.GetAsset(f.Replace('\\', '/'));
                    if (ai != null)
                    {
                        var b = AssetDatabase.GetImplicitAssetBundleName(ai.name);
                        if (string.IsNullOrEmpty(b) || b == currentBundle)
                        {
                            deps.Add(ai.name);
                            GatherDependencies(ai, deps);
                        }
                    }
                }

                foreach (var f in AssetDatabase.GetSubFolders(a.name))
                {
                    var b = AssetDatabase.GetImplicitAssetBundleName(f);
                    if (string.IsNullOrEmpty(b) || b == currentBundle)
                        GatherDependencies(AssetBundleState.GetAsset(f), deps);
                }
            }
        }
        */
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

        protected override void ContextClickedItem(int id)
        {
            var i = TreeViewUtility.FindItem(id, rootItem);
            if (i != null)
            {
                GenericMenu menu = new GenericMenu();
                foreach (var b in AssetBundleState.bundles)
                    menu.AddItem(new GUIContent("Move to bundle/" + b.Key), false, MoveToBundle, b.Value);
                menu.AddItem(new GUIContent("Move to bundle/<Create New Bundle...>"), false, MoveToBundle, null);
                menu.ShowAsContext();
            }
        }

        void MoveToBundle(object target)
        {
            AssetBundleState.BundleInfo bi = target as AssetBundleState.BundleInfo;
            if (bi == null)
                bi = AssetBundleState.CreateEmptyBundle(null);

            AssetBundleState.MoveAssetsToBundle(bi, GetRowsFromIDs(GetSelection()).Select(a => a.userData as AssetBundleState.AssetInfo));
            m_bundleTree.Refresh();
        }

    }
}
