using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

namespace UnityEngine.AssetBundles
{
	public class AssetBundleBrowserWindow : EditorWindow
	{
        [SerializeField]
        TreeViewState m_bundleTreeState;
        [SerializeField]
        TreeViewState m_assetListState;
        [SerializeField]
        TreeViewState m_selectionTreeState;

        AssetBundleTree m_bundleTree;
        AssetListTree m_assetList;
        SelectionListTree m_selectionList;
        bool m_resizingHorizontalSplitter = false;
        bool m_resizingVerticalSplitter = false;
        Rect m_horizontalSplitterRect, m_verticalSplitterRect;
        const float toolbarHeight = 5;
        const float splitterWidth = 3;
		[MenuItem("AssetBundles/Manage", priority = 0)]
		static void ShowWindow()
		{
			var window = GetWindow<AssetBundleBrowserWindow>();
			window.titleContent = new GUIContent("ABManage");
			window.Show();
        }

        [MenuItem("AssetBundles/Reset", priority = 10)]
        static void ResetAllBundles()
        {
            if (EditorUtility.DisplayDialog("Asset Bundle Reset Confirmation", "Do you want to reset ALL AssetBundle data for this project?", "Yes", "No"))
            {
                foreach (var a in AssetDatabase.GetAllAssetPaths())
                {
                    var i = AssetImporter.GetAtPath(a);
                    if (i != null && !string.IsNullOrEmpty(i.assetBundleName))
                        i.SetAssetBundleNameAndVariant(string.Empty, string.Empty);
                }

                foreach (var b in AssetDatabase.GetAllAssetBundleNames())
                    AssetDatabase.RemoveAssetBundleName(b, true);
                AssetDatabase.RemoveUnusedAssetBundleNames();
                AssetBundleState.Rebuild();
            }
        }


        void OnEnable()
        {
            m_horizontalSplitterRect = new Rect(position.width / 2, toolbarHeight, splitterWidth, this.position.height - toolbarHeight);
            m_verticalSplitterRect = new Rect(0, position.width / 2, (this.position.width - m_horizontalSplitterRect.width) - splitterWidth, splitterWidth);
        }

        private void Update()
        {
            if (AssetBundleState.CheckAndClearDirtyFlag())
            {
                if (m_bundleTree != null)
                {
                    m_bundleTree.Refresh();
                    Repaint();
                }
            }
        }

        void OnGUI()
        {
            if (m_bundleTree == null)
			{
                if (m_selectionTreeState == null)
                    m_selectionTreeState = new TreeViewState();
                m_selectionList = new SelectionListTree(m_selectionTreeState);
                m_selectionList.Reload();

                if (m_assetListState == null)
					m_assetListState = new TreeViewState();
				m_assetList = new AssetListTree(m_assetListState, m_selectionList);
                m_assetList.Reload();


                if (m_bundleTreeState == null)
					m_bundleTreeState = new TreeViewState();
				m_bundleTree = new AssetBundleTree(m_bundleTreeState, m_assetList);
                m_bundleTree.Refresh();
                m_resizingHorizontalSplitter = true; //orce a repaint
            }

            HandleHorizontalResize();
            HandleVerticalResize();

            if (EditorGUI.Button(new Rect(0, toolbarHeight, m_horizontalSplitterRect.x, 25), new GUIContent("New Bundle")))
                AssetBundleState.CreateEmptyBundle(null);
            
            m_bundleTree.OnGUI(new Rect(0, toolbarHeight + 25 + splitterWidth, m_horizontalSplitterRect.x, position.height - (toolbarHeight * 2 + splitterWidth * 2)));
            float panelLeft = m_horizontalSplitterRect.x + splitterWidth;
            float panelWidth = (position.width - m_horizontalSplitterRect.x) - splitterWidth * 2;
            float panelHeight = m_verticalSplitterRect.y - toolbarHeight;
            m_assetList.OnGUI(new Rect(panelLeft, toolbarHeight, panelWidth, panelHeight));
            m_selectionList.OnGUI(new Rect(panelLeft, m_verticalSplitterRect.y + splitterWidth, panelWidth, (position.height - m_verticalSplitterRect.y) - splitterWidth * 2));

            if (m_resizingHorizontalSplitter || m_resizingVerticalSplitter)
                Repaint();
        }

        private void HandleHorizontalResize()
        {
            m_horizontalSplitterRect.x = Mathf.Clamp(m_horizontalSplitterRect.x, position.width * .1f, (position.width - splitterWidth) * .9f);
            m_horizontalSplitterRect.height = position.height - toolbarHeight;

            EditorGUIUtility.AddCursorRect(m_horizontalSplitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.mouseDown && m_horizontalSplitterRect.Contains(Event.current.mousePosition))
                m_resizingHorizontalSplitter = true;

            if (m_resizingHorizontalSplitter)
                m_horizontalSplitterRect.x = Mathf.Clamp(Event.current.mousePosition.x, position.width * .1f, (position.width - splitterWidth) * .9f);

            if (Event.current.type == EventType.MouseUp)
                m_resizingHorizontalSplitter = false;
        }

        private void HandleVerticalResize()
        {
            m_verticalSplitterRect.x = m_horizontalSplitterRect.x;
            m_verticalSplitterRect.y = Mathf.Clamp(m_verticalSplitterRect.y, (position.height - toolbarHeight) * .1f + toolbarHeight, (position.height - splitterWidth) * .9f);
            m_verticalSplitterRect.width = position.width - m_horizontalSplitterRect.x;

            EditorGUIUtility.AddCursorRect(m_verticalSplitterRect, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.mouseDown && m_verticalSplitterRect.Contains(Event.current.mousePosition))
                m_resizingVerticalSplitter = true;

            if (m_resizingVerticalSplitter)
                m_verticalSplitterRect.y = Mathf.Clamp(Event.current.mousePosition.y, (position.height - toolbarHeight) * .1f + toolbarHeight, (position.height - splitterWidth) * .9f);

            if (Event.current.type == EventType.MouseUp)
                m_resizingVerticalSplitter = false;
        }
    }
}