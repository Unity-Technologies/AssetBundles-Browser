using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.UI;
using UnityEditor.IMGUI.Controls;
using System;

namespace UnityEngine.AssetBundles.Old
{
	internal class AssetBundleBrowserTree : TreeView
	{
		Texture2D variantIconTexture;
		internal class TreeItem : TreeViewItem
		{
			public AssetBundleData.AssetTreeItemData data;
			public TreeItem(AssetBundleData.AssetTreeItemData d, int depth) : base(d == null ? 0 : d.id, depth, d == null ? string.Empty : d.displayName)
			{
				data = d;
				if (data.assetInfo.type == AssetBundleData.AssetInfo.Type.Asset)
					icon = AssetDatabase.GetCachedIcon(data.assetInfo.assetName) as Texture2D;
				else if (data.assetInfo.type == AssetBundleData.AssetInfo.Type.Bundle)
				{
					if(data.assetInfo.isVariant)
						icon = AssetDatabase.GetCachedIcon("Assets/UnityEngine.AssetBundles/Editor/AssetBundleBrowser/variant.png") as Texture2D;
					else
						icon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName) as Texture2D;
				}

			}
	}
		public AssetBundleData assetBundleData;


		public AssetBundleBrowserTree(TreeViewState treeViewState, AssetBundleData data) : base(treeViewState)
		{
			//s_Styles = new Styles();
			variantIconTexture = Resources.Load<Texture2D>("variant.png");
			assetBundleData = data;
		}

		protected override void BuildRootAndRows(out TreeViewItem root, out IList<TreeViewItem> rows)
		{
			if (!assetBundleData.isValid)
			{
				root = new TreeViewItem(0);
				rows = new List<TreeViewItem>();
				return;
			}

			root = new TreeItem(assetBundleData.rootTreeItem, -1);
			rows = new List<TreeViewItem>();

			foreach (var i in assetBundleData.rootTreeItem.children)
				CreateAssetItem(i, 0, rows);

			//TreeViewUtility.SetParentAndChildrenForItems(rows, root);
			SetupParentsAndChildrenFromDepths(root, rows);
			//SetupDepthsFromParentsAndChildren(root);
		}

		private void CreateAssetItem(AssetBundleData.AssetTreeItemData itemData, int depth, IList<TreeViewItem> rows)
		{
			var assetItem = new TreeItem(itemData, depth);
			rows.Add(assetItem);
			if (IsExpanded(assetItem.id))
			{
				foreach (var dep in itemData.children)
					CreateAssetItem(dep, depth + 1, rows);
			}
			else
			{
				if (itemData.children.Count > 0)
					assetItem.children = CreateChildListForCollapsedParent();
			}
		}
		public delegate void SelectionChangedHandler(IList<TreeViewItem> selectedIds);
		public SelectionChangedHandler OnSelectionChanged;
		// Detect selection changes in the tree view.
		protected override void SelectionChanged(IList<int> selectedIds)
		{
            if(OnSelectionChanged != null)
                OnSelectionChanged(GetRowsFromIDs(selectedIds));
        }

	}
}