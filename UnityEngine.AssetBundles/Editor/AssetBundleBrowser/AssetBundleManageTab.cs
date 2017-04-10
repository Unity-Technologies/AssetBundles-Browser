using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;


namespace UnityEngine.AssetBundles
{
    [System.Serializable]
	public class AssetBundleManageTab 
	{
        //TODO - serialization is broken. 
        [SerializeField]
        TreeViewState m_bundleTreeState;
        [SerializeField]
        TreeViewState m_assetListState;
        [SerializeField]
        MultiColumnHeaderState m_assetListMCHState;

        Rect m_position;

        AssetBundleTree m_bundleTree;
        AssetListTree m_assetList;
        MessageList m_messageList;
        bool m_resizingHorizontalSplitter = false;
        bool m_resizingVerticalSplitter = false;
        Rect m_horizontalSplitterRect, m_verticalSplitterRect;
        [SerializeField]
        float m_horizontalSplitterPercent;
        [SerializeField]
        float m_verticalSplitterPercent;
        const float kSplitterWidth = 3;
        

        EditorWindow m_parent = null;

        public AssetBundleManageTab()
        {
            m_horizontalSplitterPercent = 0.4f;
            m_verticalSplitterPercent = 0.7f;
        }

        public void OnEnable(Rect pos, EditorWindow parent)
        {
            m_parent = parent;
            m_position = pos;
            m_horizontalSplitterRect = new Rect(
                (int)(m_position.x + m_position.width * m_horizontalSplitterPercent),
                m_position.y,
                kSplitterWidth,
                m_position.height);
            m_verticalSplitterRect = new Rect(
                m_position.x,
                (int)(m_position.y + m_horizontalSplitterRect.height * m_verticalSplitterPercent),
                (m_position.width - m_horizontalSplitterRect.width) - kSplitterWidth,
                kSplitterWidth);
        }


        private static float m_updateDelay = 0;

        public void Update()
        {
            if(Time.realtimeSinceStartup - m_updateDelay > 0.1f)
            {
                m_updateDelay = Time.realtimeSinceStartup;

                if(AssetBundleModel.Model.Update())
                {
                    m_parent.Repaint();
                }
                
                if (m_assetList != null)
                    m_assetList.Update();
            }
        }

        public void ForceReloadData()
        {
            AssetBundleModel.Model.ForceReloadData(m_bundleTree);
            m_parent.Repaint();
        }

        public void OnGUI(Rect pos)
        {
            m_position = pos;

            if(m_bundleTree == null)
            {
                if (m_assetListState == null)
                    m_assetListState = new TreeViewState();

                var headerState = AssetListTree.CreateDefaultMultiColumnHeaderState();// multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_assetListMCHState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_assetListMCHState, headerState);
                m_assetListMCHState = headerState;


                m_assetList = new AssetListTree(m_assetListState, m_assetListMCHState, this);
                m_assetList.Reload();
                m_messageList = new MessageList();

                if (m_bundleTreeState == null)
                    m_bundleTreeState = new TreeViewState();
                m_bundleTree = new AssetBundleTree(m_bundleTreeState, this);
                m_bundleTree.Refresh();
                m_parent.Repaint();
            }
            
            HandleHorizontalResize();
            HandleVerticalResize();

            var bundleTreeRect = new Rect(
                   m_position.x,
                   m_position.y,
                   m_horizontalSplitterRect.x,
                   m_position.height - kSplitterWidth);
            m_bundleTree.OnGUI(bundleTreeRect);
            if (AssetBundleModel.Model.BundleListIsEmpty())
            {
                var style = GUI.skin.label;
                style.alignment = TextAnchor.MiddleCenter;
                style.wordWrap = true;
                GUI.Label(
                    new Rect(bundleTreeRect.x + 1f, bundleTreeRect.y + 1f, bundleTreeRect.width - 2f, bundleTreeRect.height - 2f), 
                    new GUIContent(AssetBundleModel.Model.GetEmptyMessage()),
                    style);
            }

            float panelLeft = m_horizontalSplitterRect.x + kSplitterWidth;
            float panelWidth = m_verticalSplitterRect.width - kSplitterWidth * 2;
            float panelHeight = m_verticalSplitterRect.y - m_position.y;
            m_assetList.OnGUI(new Rect(
                panelLeft,
                m_position.y,
                panelWidth,
                panelHeight));
            m_messageList.OnGUI(new Rect(
                panelLeft,
                m_position.y + panelHeight + kSplitterWidth,
                panelWidth,
                (m_position.height - panelHeight) - kSplitterWidth * 2));

            if (m_resizingHorizontalSplitter || m_resizingVerticalSplitter)
                m_parent.Repaint();
        }

        private void HandleHorizontalResize()
        {
            //m_horizontalSplitterRect.x = Mathf.Clamp(m_horizontalSplitterRect.x, position.width * .1f, (position.width - kSplitterWidth) * .9f);
            m_horizontalSplitterRect.x = (int)(m_position.width * m_horizontalSplitterPercent);
            m_horizontalSplitterRect.height = m_position.height;

            EditorGUIUtility.AddCursorRect(m_horizontalSplitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.mouseDown && m_horizontalSplitterRect.Contains(Event.current.mousePosition))
                m_resizingHorizontalSplitter = true;

            if (m_resizingHorizontalSplitter)
            {
                //m_horizontalSplitterRect.x = Mathf.Clamp(Event.current.mousePosition.x, position.width * .1f, (position.width - kSplitterWidth) * .9f);
                m_horizontalSplitterPercent = Mathf.Clamp(Event.current.mousePosition.x / m_position.width, 0.1f, 0.9f);
                m_horizontalSplitterRect.x = (int)(m_position.width * m_horizontalSplitterPercent);
            }

            if (Event.current.type == EventType.MouseUp)
                m_resizingHorizontalSplitter = false;
        }

        private void HandleVerticalResize()
        {
            m_verticalSplitterRect.x = m_horizontalSplitterRect.x;
            //m_verticalSplitterRect.y = Mathf.Clamp(m_verticalSplitterRect.y, (position.height - toolbarPadding) * .1f + toolbarPadding, (position.height - kSplitterWidth) * .9f);
            m_verticalSplitterRect.y = (int)(m_horizontalSplitterRect.height * m_verticalSplitterPercent);
            m_verticalSplitterRect.width = m_position.width - m_horizontalSplitterRect.x;

            EditorGUIUtility.AddCursorRect(m_verticalSplitterRect, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.mouseDown && m_verticalSplitterRect.Contains(Event.current.mousePosition))
                m_resizingVerticalSplitter = true;

            if (m_resizingVerticalSplitter)
            {
                //m_verticalSplitterRect.y = Mathf.Clamp(Event.current.mousePosition.y, (position.height - toolbarPadding) * .1f + toolbarPadding, (position.height - kSplitterWidth) * .9f);
                m_verticalSplitterPercent = Mathf.Clamp(Event.current.mousePosition.y / m_horizontalSplitterRect.height, 0.2f, 0.98f);
                m_verticalSplitterRect.y = (int)(m_horizontalSplitterRect.height * m_verticalSplitterPercent);
            }

                if (Event.current.type == EventType.MouseUp)
                m_resizingVerticalSplitter = false;
        }

        public void UpdateSelectedBundles(IEnumerable<AssetBundleModel.BundleInfo> bundles)
        {
            AssetBundleModel.Model.AddBundlesToUpdate(bundles);
            m_assetList.SetSelectedBundles(bundles);
            m_messageList.SetItems(null);
        }

        public void SetSelectedItems(IEnumerable<AssetBundleModel.AssetInfo> items)
        {
            m_messageList.SetItems(items);
            //m_selectionList.SetItems(items);
        }
    }
}