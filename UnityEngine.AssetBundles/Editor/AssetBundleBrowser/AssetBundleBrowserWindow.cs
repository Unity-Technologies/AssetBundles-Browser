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
        const float toolbarHeight = 20;
        const float splitterWidth = 3;
        public static List<AssetBundleBrowserWindow> s_openWindows = new List<AssetBundleBrowserWindow>();
		[MenuItem("Window/Asset Bundle Browser")]
		static void ShowWindow()
		{
			var window = GetWindow<AssetBundleBrowserWindow>();
			window.titleContent = new GUIContent("AssetBundles");
			window.Show();
			s_openWindows.Add(window);
		}

        void OnDestroy()
        {
            s_openWindows.Remove(this);
        }

        void OnEnable()
        {
            m_horizontalSplitterRect = new Rect(position.width / 2, toolbarHeight, splitterWidth, this.position.height - toolbarHeight);
            m_verticalSplitterRect = new Rect(0, position.width / 2, (this.position.width - m_horizontalSplitterRect.width) - splitterWidth, splitterWidth);
        }

		void OnGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh State", GUILayout.Height(toolbarHeight)))
                AssetBundleState.Rebuild();

            if (GUILayout.Button("Revert Changes", GUILayout.Height(toolbarHeight)))
                AssetBundleState.ClearChanges();

            if (GUILayout.Button("Apply Changes", GUILayout.Height(toolbarHeight)))
                AssetBundleState.ApplyChanges();
            GUILayout.EndHorizontal();

            if (m_bundleTree == null)
			{
                if (m_selectionTreeState == null)
                    m_selectionTreeState = new TreeViewState();
                m_selectionList = new SelectionListTree(m_selectionTreeState);

				if (m_assetListState == null)
					m_assetListState = new TreeViewState();
				m_assetList = new AssetListTree(m_assetListState, m_selectionList);

				if (m_bundleTreeState == null)
					m_bundleTreeState = new TreeViewState();
				m_bundleTree = new AssetBundleTree(m_bundleTreeState, m_assetList);
			}
            if (AssetBundleState.CheckAndClearDirtyFlag())
                m_bundleTree.Reload();
            HandleHorizontalResize();
            HandleVerticalResize();

			m_bundleTree.OnGUI(new Rect(0, toolbarHeight, m_horizontalSplitterRect.x, position.height - toolbarHeight));
            float panelLeft = m_horizontalSplitterRect.x + splitterWidth;
            float panelWidth = (position.width - m_horizontalSplitterRect.x) - splitterWidth;
            float panelHeight = m_verticalSplitterRect.y - toolbarHeight;
            m_assetList.OnGUI(new Rect(panelLeft, toolbarHeight, panelWidth, panelHeight));
            m_selectionList.OnGUI(new Rect(panelLeft, m_verticalSplitterRect.y + splitterWidth, panelWidth, (position.height - m_verticalSplitterRect.y) - splitterWidth));

            if (m_resizingHorizontalSplitter || m_resizingVerticalSplitter)
                Repaint();
        }

        internal void Refresh()
        {
            m_bundleTree.Refresh();
        }

        private void HandleHorizontalResize()
        {
            m_horizontalSplitterRect.x = Mathf.Clamp(m_horizontalSplitterRect.x, position.width * .1f, (position.width - splitterWidth) * .9f);
            m_horizontalSplitterRect.height = position.height - toolbarHeight;

            GUI.DrawTexture(m_horizontalSplitterRect, EditorGUIUtility.whiteTexture);
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

            GUI.DrawTexture(m_verticalSplitterRect, EditorGUIUtility.whiteTexture);
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