using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;


namespace UnityEngine.AssetBundles
{
    [System.Serializable]
    public class AssetBundleManageTab 
    {
        [SerializeField]
        TreeViewState m_BundleTreeState;
        [SerializeField]
        TreeViewState m_AssetListState;
        [SerializeField]
        MultiColumnHeaderState m_AssetListMCHState;
        [SerializeField]
        TreeViewState m_BundleDetailState;

        Rect m_Position;

        AssetBundleTree m_BundleTree;
        AssetListTree m_AssetList;
        MessageList m_MessageList;
        BundleDetailList m_DetailsList;
        AssetBundleOperation.ABOperation m_Operation;
        bool m_ResizingHorizontalSplitter = false;
        bool m_ResizingVerticalSplitterRight = false;
        bool m_ResizingVerticalSplitterLeft = false;
        Rect m_HorizontalSplitterRect, m_VerticalSplitterRectRight, m_VerticalSplitterRectLeft;
        [SerializeField]
        float m_HorizontalSplitterPercent;
        [SerializeField]
        float m_VerticalSplitterPercentRight;
        [SerializeField]
        float m_VerticalSplitterPercentLeft;
        const float k_SplitterWidth = 3f;
        const float k_BundleTreeMenu = 20f;
        private static float m_UpdateDelay = 0f;

        public AssetBundleOperation.ABOperation Operation {
            get { return m_Operation; }
        }

        EditorWindow m_Parent = null;

        public AssetBundleManageTab()
        {
            m_HorizontalSplitterPercent = 0.4f;
            m_VerticalSplitterPercentRight = 0.7f;
            m_VerticalSplitterPercentLeft = 0.85f;
            m_Operation = new AssetBundleOperation.AssetDatabaseABOperation ();
        }

        public void OnEnable(Rect pos, EditorWindow parent)
        {
            m_Parent = parent;
            m_Position = pos;
            m_HorizontalSplitterRect = new Rect(
                (int)(m_Position.x + m_Position.width * m_HorizontalSplitterPercent),
                m_Position.y,
                k_SplitterWidth,
                m_Position.height);
            m_VerticalSplitterRectRight = new Rect(
                m_HorizontalSplitterRect.x,
                (int)(m_Position.y + m_HorizontalSplitterRect.height * m_VerticalSplitterPercentRight),
                (m_Position.width - m_HorizontalSplitterRect.width) - k_SplitterWidth,
                k_SplitterWidth);
            m_VerticalSplitterRectLeft = new Rect(
                m_Position.x,
                (int)(m_Position.y + m_HorizontalSplitterRect.height * m_VerticalSplitterPercentLeft),
                (m_HorizontalSplitterRect.width) - k_SplitterWidth,
                k_SplitterWidth);
        }



        public void Update()
        {
            if(Time.realtimeSinceStartup - m_UpdateDelay > 0.1f)
            {
                m_UpdateDelay = Time.realtimeSinceStartup;

                if(AssetBundleModel.Model.Update())
                {
                    m_Parent.Repaint();
                }

                if (m_DetailsList != null)
                    m_DetailsList.Update();

                if (m_AssetList != null)
                    m_AssetList.Update();

            }
        }

        public void ForceReloadData()
        {
            AssetBundleModel.Model.ForceReloadData(m_BundleTree, m_Operation);
            m_Parent.Repaint();
        }

        public void OnGUI(Rect pos)
        {
            m_Position = pos;

            if(m_BundleTree == null)
            {
                if (m_AssetListState == null)
                    m_AssetListState = new TreeViewState();

                var headerState = AssetListTree.CreateDefaultMultiColumnHeaderState();// multiColumnTreeViewRect.width);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_AssetListMCHState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_AssetListMCHState, headerState);
                m_AssetListMCHState = headerState;


                m_AssetList = new AssetListTree(m_AssetListState, m_AssetListMCHState, this);
                m_AssetList.Reload();
                m_MessageList = new MessageList();

                if (m_BundleDetailState == null)
                    m_BundleDetailState = new TreeViewState();
                m_DetailsList = new BundleDetailList(m_BundleDetailState);
                m_DetailsList.Reload();

                if (m_BundleTreeState == null)
                    m_BundleTreeState = new TreeViewState();
                m_BundleTree = new AssetBundleTree(m_BundleTreeState, this);
                m_BundleTree.Refresh();
                m_Parent.Repaint();
            }
            
            HandleHorizontalResize();
            HandleVerticalResize();


            if (AssetBundleModel.Model.BundleListIsEmpty())
            {
                m_BundleTree.OnGUI(m_Position);
                var style = GUI.skin.label;
                style.alignment = TextAnchor.MiddleCenter;
                style.wordWrap = true;
                GUI.Label(
                    new Rect(m_Position.x + 1f, m_Position.y + 1f, m_Position.width - 2f, m_Position.height - 2f), 
                    new GUIContent(AssetBundleModel.Model.GetEmptyMessage()),
                    style);
            }
            else
            {
                var bundleTreeMenu = new Rect(
                    m_Position.x, 
                    m_Position.y, 
                    m_HorizontalSplitterRect.x, 
                    k_BundleTreeMenu);

                //Left half
                var bundleTreeRect = new Rect(
                    bundleTreeMenu.x,
                    bundleTreeMenu.y + bundleTreeMenu.height,
                    bundleTreeMenu.width,
                    m_VerticalSplitterRectLeft.y - bundleTreeMenu.y);
                
                DrawBundleTreeToolBarGUI (bundleTreeMenu);
                m_BundleTree.OnGUI(bundleTreeRect);
                m_DetailsList.OnGUI(new Rect(
                    bundleTreeRect.x,
                    bundleTreeRect.y + bundleTreeRect.height + k_SplitterWidth,
                    bundleTreeRect.width,
                    m_Position.height - bundleTreeRect.height - k_SplitterWidth*2));


                //Right half.
                float panelLeft = m_HorizontalSplitterRect.x + k_SplitterWidth;
                float panelWidth = m_VerticalSplitterRectRight.width - k_SplitterWidth * 2;
                float panelHeight = m_VerticalSplitterRectRight.y - m_Position.y;
                m_AssetList.OnGUI(new Rect(
                    panelLeft,
                    m_Position.y,
                    panelWidth,
                    panelHeight));
                m_MessageList.OnGUI(new Rect(
                    panelLeft,
                    m_Position.y + panelHeight + k_SplitterWidth,
                    panelWidth,
                    (m_Position.height - panelHeight) - k_SplitterWidth * 2));

                if (m_ResizingHorizontalSplitter || m_ResizingVerticalSplitterRight || m_ResizingVerticalSplitterLeft)
                    m_Parent.Repaint();
            }
        }

        private void DrawBundleTreeToolBarGUI(Rect r) {

            GUILayout.BeginArea (r);

            using (new EditorGUILayout.HorizontalScope (EditorStyles.toolbar)) {

                if (GUILayout.Button (new GUIContent (string.Format("{0} ({1})", m_Operation.Name, m_Operation.ProviderName), "Select Asset Bundle Set"), 
                    EditorStyles.toolbarPopup, GUILayout.Width (200f), GUILayout.Height (r.height))) 
                {
                    GenericMenu menu = new GenericMenu ();
                    bool firstItem = true;

                    foreach (var info in AssetBundleOperation.ABOperationProviderUtility.CustomABOperationProviderTypes) {
                        var newProvider = info.CreateInstance();

                        if (!firstItem) {
                            menu.AddSeparator ("");
                        }

                        for (int i = 0; i < newProvider.GetABOperationCount (); ++i) {
                            var op = newProvider.CreateOperation (i);

                            menu.AddItem (new GUIContent (string.Format("{0} ({1})", op.Name, op.ProviderName)), false, 
                                () => {
                                    var thisOperation = op;
                                    m_Operation = thisOperation;
                                    ForceReloadData();
                                }
                            );
                        }

                        firstItem = false;
                    }

                    menu.DropDown(new Rect(4f, 8f, 0f, 0f));
                }

                GUILayout.FlexibleSpace ();
                if (m_Operation.IsReadOnly ()) {
                    GUIStyle tbLabel = new GUIStyle(EditorStyles.toolbar);
                    tbLabel.alignment = TextAnchor.MiddleRight;

                    GUILayout.Label ("Read Only", tbLabel, GUILayout.Width(60f), GUILayout.Height (r.height));
                }
            }

            GUILayout.EndArea ();
        }

        private void HandleHorizontalResize()
        {
            //m_horizontalSplitterRect.x = Mathf.Clamp(m_horizontalSplitterRect.x, position.width * .1f, (position.width - kSplitterWidth) * .9f);
            m_HorizontalSplitterRect.x = (int)(m_Position.width * m_HorizontalSplitterPercent);
            m_HorizontalSplitterRect.height = m_Position.height;

            EditorGUIUtility.AddCursorRect(m_HorizontalSplitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.mouseDown && m_HorizontalSplitterRect.Contains(Event.current.mousePosition))
                m_ResizingHorizontalSplitter = true;

            if (m_ResizingHorizontalSplitter)
            {
                //m_horizontalSplitterRect.x = Mathf.Clamp(Event.current.mousePosition.x, position.width * .1f, (position.width - kSplitterWidth) * .9f);
                m_HorizontalSplitterPercent = Mathf.Clamp(Event.current.mousePosition.x / m_Position.width, 0.1f, 0.9f);
                m_HorizontalSplitterRect.x = (int)(m_Position.width * m_HorizontalSplitterPercent);
            }

            if (Event.current.type == EventType.MouseUp)
                m_ResizingHorizontalSplitter = false;
        }

        private void HandleVerticalResize()
        {
            m_VerticalSplitterRectRight.x = m_HorizontalSplitterRect.x;
            m_VerticalSplitterRectRight.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentRight);
            m_VerticalSplitterRectRight.width = m_Position.width - m_HorizontalSplitterRect.x;
            m_VerticalSplitterRectLeft.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentLeft);
            m_VerticalSplitterRectLeft.width = m_VerticalSplitterRectRight.width;


            EditorGUIUtility.AddCursorRect(m_VerticalSplitterRectRight, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.mouseDown && m_VerticalSplitterRectRight.Contains(Event.current.mousePosition))
                m_ResizingVerticalSplitterRight = true;

            EditorGUIUtility.AddCursorRect(m_VerticalSplitterRectLeft, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.mouseDown && m_VerticalSplitterRectLeft.Contains(Event.current.mousePosition))
                m_ResizingVerticalSplitterLeft = true;


            if (m_ResizingVerticalSplitterRight)
            {
                m_VerticalSplitterPercentRight = Mathf.Clamp(Event.current.mousePosition.y / m_HorizontalSplitterRect.height, 0.2f, 0.98f);
                m_VerticalSplitterRectRight.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentRight);
            }
            else if (m_ResizingVerticalSplitterLeft)
            {
                m_VerticalSplitterPercentLeft = Mathf.Clamp(Event.current.mousePosition.y / m_HorizontalSplitterRect.height, 0.25f, 0.98f);
                m_VerticalSplitterRectLeft.y = (int)(m_HorizontalSplitterRect.height * m_VerticalSplitterPercentLeft);
            }


            if (Event.current.type == EventType.MouseUp)
            {
                m_ResizingVerticalSplitterRight = false;
                m_ResizingVerticalSplitterLeft = false;
            }
        }

        public void UpdateSelectedBundles(IEnumerable<AssetBundleModel.BundleInfo> bundles)
        {
            AssetBundleModel.Model.AddBundlesToUpdate(bundles);
            m_AssetList.SetSelectedBundles(bundles);
            m_DetailsList.SetItems(bundles);
            m_MessageList.SetItems(null);
        }

        public void SetSelectedItems(IEnumerable<AssetBundleModel.AssetInfo> items)
        {
            m_MessageList.SetItems(items);
        }
    }
}