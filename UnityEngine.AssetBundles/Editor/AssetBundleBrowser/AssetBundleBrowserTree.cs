using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.UI;
using UnityEditor.IMGUI.Controls;
using System;

namespace UnityEditor.AssetBundles
{

	internal class AssetBundleBrowserTree : TreeView
	{
		internal class TreeItem : TreeViewItem
		{
			public AssetBundleData.AssetTreeItemData data;
			public TreeItem(AssetBundleData.AssetTreeItemData d, int depth) : base(d == null ? 0 : d.id, depth, d == null ? string.Empty : d.displayName)
			{
				data = d;
			}
		}
		public AssetBundleData assetBundleData;


		public AssetBundleBrowserTree(TreeViewState treeViewState, AssetBundleData data) : base(treeViewState)
		{
			s_Styles = new Styles();
			assetBundleData = data;
		}

		protected override void BuildRootAndRows(out TreeViewItem root, out IList<TreeViewItem> rows)
		{
			root = new TreeItem(null, -1);
			rows = new List<TreeViewItem>();

			foreach (var i in assetBundleData.treeItemData)
				CreateAssetItem(i, 0, rows);

			TreeViewUtility.SetParentAndChildrenForItems(rows, root);
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
		public delegate void SelectionChangedHandler(IList<int> selectedIds);
		public SelectionChangedHandler OnSelectionChanged;
		// Detect selection changes in the tree view.
		protected override void SelectionChanged(IList<int> selectedIds)
		{
			if (OnSelectionChanged != null)
				OnSelectionChanged(selectedIds);
		}

		protected override void OnItemGUI(ItemGUIEventArgs args)
		{
			if (Event.current.rawType != EventType.Repaint)
				return;

			Rect rect = args.rowRect;
			GUIStyle lineStyle = s_Styles.lineStyle;
			rect.xMin += lineStyle.margin.left + k_BaseIndent + args.item.depth * indentWidth + s_Styles.foldout.fixedWidth;
			lineStyle.padding.left = 0;

			AssetBundleData.AssetInfo asset = (args.item as TreeItem).data.assetInfo;
			if (asset != null)
			{
				Rect iconRect = rect;
				iconRect.width = k_IconWidth;
				iconRect.x += iconLeftPadding;
				rect.xMin += k_IconWidth + iconRightPadding;
				if(asset.type == AssetBundleData.AssetInfo.Type.Bundle)
					GUI.DrawTexture(iconRect, EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName), ScaleMode.ScaleToFit);
				else
					GUI.DrawTexture(iconRect, AssetDatabase.GetCachedIcon(asset.assetName), ScaleMode.ScaleToFit);
			}
			lineStyle.Draw(rect, args.item.displayName, false, false, args.selected, args.focused);
		}


		public float iconLeftPadding { get; set; }
		public float iconRightPadding { get { return 2; } set { } }
		public float iconTotalPadding { get { return iconLeftPadding + iconRightPadding; } }

		// Layout
		public float k_LineHeight = 16f;
		public float k_BaseIndent = 2f;
		public float k_IndentWidth = 14f;
		public float k_IconWidth = 16f;
		public float k_SpaceBetweenIconAndText = 2f;
		public float k_TopRowMargin = 0f;
		public float k_BottomRowMargin = 0f;
		public float indentWidth { get { return k_IndentWidth + iconTotalPadding; } }
		public float k_HalfDropBetweenHeight = 4f;
		public float foldoutYOffset = 0f;
		public float extraInsertionMarkerIndent = 0f;

		// Styles
		internal class Styles
		{
			public GUIStyle foldout = "IN Foldout";
			//public GUIStyle ping = new GUIStyle("PR Ping");
			public GUIStyle lineStyle = new GUIStyle("PR Label");
			//public GUIContent content = new GUIContent(EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName));

			public Styles()
			{
				// We want to render selection separately from text and icon, so clear background textures
				// TODO: Fix in new style setup
				var transparent = lineStyle.hover.background;
				lineStyle.onNormal.background = transparent;
				lineStyle.onActive.background = transparent;
				lineStyle.onFocused.background = transparent;
				lineStyle.richText = true;
				lineStyle.alignment = TextAnchor.MiddleLeft;
				lineStyle.fixedHeight = 0;
			}
		}
		protected static Styles s_Styles;
	}
}