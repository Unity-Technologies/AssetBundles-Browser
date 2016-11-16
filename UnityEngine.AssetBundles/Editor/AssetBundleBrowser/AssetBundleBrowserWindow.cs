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

        public static List<AssetBundleBrowserWindow> s_openWindows = new List<AssetBundleBrowserWindow>();
		[MenuItem("Window/Asset Bundle Browser V2")]
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
            m_horizontalSplitterRect = new Rect(position.width / 2, 0, 5f, this.position.height);
            m_verticalSplitterRect = new Rect(0, position.width / 2, (this.position.width - m_horizontalSplitterRect.width) - 5, 5f);
        }

		void OnGUI()
		{
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

            HandleHorizontalResize();
            HandleVerticalResize();

			m_bundleTree.OnGUI(new Rect(0, 0, m_horizontalSplitterRect.x, position.height));
            float panelLeft = m_horizontalSplitterRect.x + 5;
            float panelWidth = (position.width - m_horizontalSplitterRect.x) - 5;

            m_assetList.OnGUI(new Rect(panelLeft, 0, panelWidth, m_verticalSplitterRect.y));
            m_selectionList.OnGUI(new Rect(panelLeft, m_verticalSplitterRect.y + 5, panelWidth, (position.height - m_verticalSplitterRect.y) - 5));

            if (m_resizingHorizontalSplitter || m_resizingVerticalSplitter)
                Repaint();
        }

        private void HandleHorizontalResize()
        {
            m_horizontalSplitterRect.x = Mathf.Clamp(m_horizontalSplitterRect.x, position.width * .1f, position.width * .9f);
            m_horizontalSplitterRect.height = position.height;

            GUI.DrawTexture(m_horizontalSplitterRect, EditorGUIUtility.whiteTexture);
            EditorGUIUtility.AddCursorRect(m_horizontalSplitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.mouseDown && m_horizontalSplitterRect.Contains(Event.current.mousePosition))
                m_resizingHorizontalSplitter = true;

            if (m_resizingHorizontalSplitter)
                m_horizontalSplitterRect.x = Mathf.Clamp(Event.current.mousePosition.x, position.width * .1f, position.width * .9f);

            if (Event.current.type == EventType.MouseUp)
                m_resizingHorizontalSplitter = false;
        }

        private void HandleVerticalResize()
        {
            m_verticalSplitterRect.x = m_horizontalSplitterRect.x;
            m_verticalSplitterRect.y = Mathf.Clamp(m_verticalSplitterRect.y, position.height * .1f, position.height * .9f);
            m_verticalSplitterRect.width = position.width - m_horizontalSplitterRect.x;

            GUI.DrawTexture(m_verticalSplitterRect, EditorGUIUtility.whiteTexture);
            EditorGUIUtility.AddCursorRect(m_verticalSplitterRect, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.mouseDown && m_verticalSplitterRect.Contains(Event.current.mousePosition))
                m_resizingVerticalSplitter = true;

            if (m_resizingVerticalSplitter)
                m_verticalSplitterRect.y = Mathf.Clamp(Event.current.mousePosition.y, position.height * .1f, position.height* .9f);

            if (Event.current.type == EventType.MouseUp)
                m_resizingVerticalSplitter = false;
        }
    }
}