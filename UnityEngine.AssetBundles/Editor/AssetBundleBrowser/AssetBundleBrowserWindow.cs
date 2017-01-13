using UnityEditor;
using UnityEditor.IMGUI.Controls;

/*
 * handle large number of bundles in dropdown list
 * new bundle button is not working
 * clean up code
 * handle context/dragging cases where the item is not of valid type
 * add support for bundle folders (make sure "folder" bundles are supported)
 * probably need an optimized dependency graph to speed things up, needs to support changes
 * dictionary based on asset name or index based?  
 */

namespace UnityEngine.AssetBundles
{
	public class AssetBundleBrowserWindow : EditorWindow
	{
        [SerializeField]
        TreeViewState m_bundleTreeState;
        [SerializeField]
        TreeViewState m_assetListState;
        [SerializeField]
        MultiColumnHeaderState m_assetListMCHState;
        [SerializeField]
        TreeViewState m_selectionTreeState;

        AssetBundleTree m_bundleTree;
        AssetListTree m_assetList;
        SelectionListTree m_selectionList;
        bool m_resizingHorizontalSplitter = false;
        bool m_resizingVerticalSplitter = false;
        Rect m_horizontalSplitterRect, m_verticalSplitterRect;
        const float kToolbarHeight = 5;
        const float kSplitterWidth = 3;
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
            m_horizontalSplitterRect = new Rect(position.width / 2, kToolbarHeight, kSplitterWidth, this.position.height - kToolbarHeight);
            m_verticalSplitterRect = new Rect(0, position.width / 2, (this.position.width - m_horizontalSplitterRect.width) - kSplitterWidth, kSplitterWidth);
        }

        private void Update()
        {
            AssetBundleState.Update();
            if (m_assetList != null)
                m_assetList.Update();
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
                {
                    m_assetListState = new TreeViewState();
                    m_assetListMCHState = new MultiColumnHeaderState(AssetListTree.GetColumns());
                }
				m_assetList = new AssetListTree(m_assetListState, m_assetListMCHState, m_selectionList);
                m_assetList.Reload();


                if (m_bundleTreeState == null)
					m_bundleTreeState = new TreeViewState();
				m_bundleTree = new AssetBundleTree(m_bundleTreeState, m_assetList);
                m_bundleTree.Refresh();
                Repaint();
            }

            HandleHorizontalResize();
            HandleVerticalResize();

            if (GUI.Button(new Rect(0, kToolbarHeight, m_horizontalSplitterRect.x/2, 25), new GUIContent("New Bundle")))
                AssetBundleState.GetBundle(null);
           // if (GUI.Button(new Rect(m_horizontalSplitterRect.x / 2, kToolbarHeight, m_horizontalSplitterRect.x / 2, 25), new GUIContent("New Folder")))
            //    ;// m_bundleTree.Add
                //    if (GUI.Button(new Rect(m_horizontalSplitterRect.x / 2, kToolbarHeight, m_horizontalSplitterRect.x/2, 25), new GUIContent("RESET")))
                //        AssetBundleState.Rebuild();

                m_bundleTree.OnGUI(new Rect(0, kToolbarHeight + 25 + kSplitterWidth, m_horizontalSplitterRect.x, position.height - (kToolbarHeight * 2 + kSplitterWidth * 2 + 25)));
            float panelLeft = m_horizontalSplitterRect.x + kSplitterWidth;
            float panelWidth = (position.width - m_horizontalSplitterRect.x) - kSplitterWidth * 2;
            float panelHeight = m_verticalSplitterRect.y - kToolbarHeight;
            m_assetList.OnGUI(new Rect(panelLeft, kToolbarHeight, panelWidth, panelHeight));
            m_selectionList.OnGUI(new Rect(panelLeft, m_verticalSplitterRect.y + kSplitterWidth, panelWidth, (position.height - m_verticalSplitterRect.y) - kSplitterWidth * 2));

            if (m_resizingHorizontalSplitter || m_resizingVerticalSplitter)
                Repaint();
        }

        private void HandleHorizontalResize()
        {
            m_horizontalSplitterRect.x = Mathf.Clamp(m_horizontalSplitterRect.x, position.width * .1f, (position.width - kSplitterWidth) * .9f);
            m_horizontalSplitterRect.height = position.height - kToolbarHeight;

            EditorGUIUtility.AddCursorRect(m_horizontalSplitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.mouseDown && m_horizontalSplitterRect.Contains(Event.current.mousePosition))
                m_resizingHorizontalSplitter = true;

            if (m_resizingHorizontalSplitter)
                m_horizontalSplitterRect.x = Mathf.Clamp(Event.current.mousePosition.x, position.width * .1f, (position.width - kSplitterWidth) * .9f);

            if (Event.current.type == EventType.MouseUp)
                m_resizingHorizontalSplitter = false;
        }

        private void HandleVerticalResize()
        {
            m_verticalSplitterRect.x = m_horizontalSplitterRect.x;
            m_verticalSplitterRect.y = Mathf.Clamp(m_verticalSplitterRect.y, (position.height - kToolbarHeight) * .1f + kToolbarHeight, (position.height - kSplitterWidth) * .9f);
            m_verticalSplitterRect.width = position.width - m_horizontalSplitterRect.x;

            EditorGUIUtility.AddCursorRect(m_verticalSplitterRect, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.mouseDown && m_verticalSplitterRect.Contains(Event.current.mousePosition))
                m_resizingVerticalSplitter = true;

            if (m_resizingVerticalSplitter)
                m_verticalSplitterRect.y = Mathf.Clamp(Event.current.mousePosition.y, (position.height - kToolbarHeight) * .1f + kToolbarHeight, (position.height - kSplitterWidth) * .9f);

            if (Event.current.type == EventType.MouseUp)
                m_resizingVerticalSplitter = false;
        }
    }
}