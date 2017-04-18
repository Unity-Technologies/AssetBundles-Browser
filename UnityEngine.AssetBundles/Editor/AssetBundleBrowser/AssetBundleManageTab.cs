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
        [SerializeField]
        TreeViewState m_bundleDetailState;

        Rect m_position;

        AssetBundleTree m_bundleTree;
        AssetListTree m_assetList;
        MessageList m_messageList;
        BundleDetailList m_detailsList;
        bool m_resizingHorizontalSplitter = false;
        bool m_resizingVerticalSplitterRight = false;
        bool m_resizingVerticalSplitterLeft = false;
        Rect m_horizontalSplitterRect, m_verticalSplitterRectRight, m_verticalSplitterRectLeft;
        [SerializeField]
        float m_horizontalSplitterPercent;
        [SerializeField]
        float m_verticalSplitterPercentRight;
        [SerializeField]
        float m_verticalSplitterPercentLeft;
        const float kSplitterWidth = 3;
        

        EditorWindow m_parent = null;

        public AssetBundleManageTab()
        {
            m_horizontalSplitterPercent = 0.4f;
            m_verticalSplitterPercentRight = 0.7f;
            m_verticalSplitterPercentLeft = 0.85f;
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
            m_verticalSplitterRectRight = new Rect(
                m_horizontalSplitterRect.x,
                (int)(m_position.y + m_horizontalSplitterRect.height * m_verticalSplitterPercentRight),
                (m_position.width - m_horizontalSplitterRect.width) - kSplitterWidth,
                kSplitterWidth);
            m_verticalSplitterRectLeft = new Rect(
                m_position.x,
                (int)(m_position.y + m_horizontalSplitterRect.height * m_verticalSplitterPercentLeft),
                (m_horizontalSplitterRect.width) - kSplitterWidth,
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

                if (m_detailsList != null)
                    m_detailsList.Update();

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

                if (m_bundleDetailState == null)
                    m_bundleDetailState = new TreeViewState();
                m_detailsList = new BundleDetailList(m_bundleDetailState);
                m_detailsList.Reload();

                if (m_bundleTreeState == null)
                    m_bundleTreeState = new TreeViewState();
                m_bundleTree = new AssetBundleTree(m_bundleTreeState, this);
                m_bundleTree.Refresh();
                m_parent.Repaint();
            }
            
            HandleHorizontalResize();
            HandleVerticalResize();


            if (AssetBundleModel.Model.BundleListIsEmpty())
            {
                m_bundleTree.OnGUI(m_position);
                var style = GUI.skin.label;
                style.alignment = TextAnchor.MiddleCenter;
                style.wordWrap = true;
                GUI.Label(
                    new Rect(m_position.x + 1f, m_position.y + 1f, m_position.width - 2f, m_position.height - 2f), 
                    new GUIContent(AssetBundleModel.Model.GetEmptyMessage()),
                    style);
            }
            else
            {
                //Left half
                var bundleTreeRect = new Rect(
                   m_position.x,
                   m_position.y,
                   m_horizontalSplitterRect.x,
                   m_verticalSplitterRectLeft.y - m_position.y);
                m_bundleTree.OnGUI(bundleTreeRect);
                m_detailsList.OnGUI(new Rect(
                    bundleTreeRect.x,
                    bundleTreeRect.y + bundleTreeRect.height + kSplitterWidth,
                    bundleTreeRect.width,
                    m_position.height - bundleTreeRect.height - kSplitterWidth*2));


                //Right half.
                float panelLeft = m_horizontalSplitterRect.x + kSplitterWidth;
                float panelWidth = m_verticalSplitterRectRight.width - kSplitterWidth * 2;
                float panelHeight = m_verticalSplitterRectRight.y - m_position.y;
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

                if (m_resizingHorizontalSplitter || m_resizingVerticalSplitterRight || m_resizingVerticalSplitterLeft)
                    m_parent.Repaint();
            }


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
            m_verticalSplitterRectRight.x = m_horizontalSplitterRect.x;
            m_verticalSplitterRectRight.y = (int)(m_horizontalSplitterRect.height * m_verticalSplitterPercentRight);
            m_verticalSplitterRectRight.width = m_position.width - m_horizontalSplitterRect.x;
            m_verticalSplitterRectLeft.y = (int)(m_horizontalSplitterRect.height * m_verticalSplitterPercentLeft);
            m_verticalSplitterRectLeft.width = m_verticalSplitterRectRight.width;


            EditorGUIUtility.AddCursorRect(m_verticalSplitterRectRight, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.mouseDown && m_verticalSplitterRectRight.Contains(Event.current.mousePosition))
                m_resizingVerticalSplitterRight = true;

            EditorGUIUtility.AddCursorRect(m_verticalSplitterRectLeft, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.mouseDown && m_verticalSplitterRectLeft.Contains(Event.current.mousePosition))
                m_resizingVerticalSplitterLeft = true;


            if (m_resizingVerticalSplitterRight)
            {
                m_verticalSplitterPercentRight = Mathf.Clamp(Event.current.mousePosition.y / m_horizontalSplitterRect.height, 0.2f, 0.98f);
                m_verticalSplitterRectRight.y = (int)(m_horizontalSplitterRect.height * m_verticalSplitterPercentRight);
            }
            else if (m_resizingVerticalSplitterLeft)
            {
                m_verticalSplitterPercentLeft = Mathf.Clamp(Event.current.mousePosition.y / m_horizontalSplitterRect.height, 0.25f, 0.98f);
                m_verticalSplitterRectLeft.y = (int)(m_horizontalSplitterRect.height * m_verticalSplitterPercentLeft);
            }


            if (Event.current.type == EventType.MouseUp)
            {
                m_resizingVerticalSplitterRight = false;
                m_resizingVerticalSplitterLeft = false;
            }
        }

        public void UpdateSelectedBundles(IEnumerable<AssetBundleModel.BundleInfo> bundles)
        {
            AssetBundleModel.Model.AddBundlesToUpdate(bundles);
            m_assetList.SetSelectedBundles(bundles);
            m_detailsList.SetItems(bundles);
            m_messageList.SetItems(null);
        }

        public void SetSelectedItems(IEnumerable<AssetBundleModel.AssetInfo> items)
        {
            m_messageList.SetItems(items);
        }
    }
}