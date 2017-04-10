using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.AssetBundles
{

    public class AssetBundleBrowserMain : EditorWindow
    {

        public const float kButtonWidth = 150;

        enum Mode
        {
            Browser,
            Builder,
        }
        [SerializeField]
        Mode m_Mode;

        [SerializeField]
        public AssetBundleManageTab m_ManageTab;

        [SerializeField]
        public AssetBundleBuildTab m_BuildTab;

        private Texture2D m_RefreshTexture;

        const float kToolbarPadding = 15;

        [MenuItem("Window/AssetBundle Browser", priority = 2050)]
        static void ShowWindow()
        {
            var window = GetWindow<AssetBundleBrowserMain>();
            window.titleContent = new GUIContent("AssetBundles");
            window.Show();
        }
        private void OnEnable()
        {

            Rect subPos = GetSubWindowArea();
            if(m_ManageTab == null)
                m_ManageTab = new AssetBundleManageTab();
            m_ManageTab.OnEnable(subPos, this);
            if(m_BuildTab == null)
                m_BuildTab = new AssetBundleBuildTab();
            m_BuildTab.OnEnable(subPos, this);

            m_RefreshTexture = EditorGUIUtility.FindTexture("Refresh");
        }

        private float toolbarPadding = 32;
        private Rect GetSubWindowArea()
        {
            Rect subPos = new Rect(0, toolbarPadding, position.width, position.height - toolbarPadding);
            return subPos;
        }

        private void Update()
        {
            switch (m_Mode)
            {
                case Mode.Builder:
                    //m_BuildTab.Update();
                    break;
                case Mode.Browser:
                default:
                    m_ManageTab.Update();
                    break;
            }
        }

        private void OnGUI()
        {
            ModeToggle();

            switch(m_Mode)
            {
                case Mode.Builder:
                    m_BuildTab.OnGUI(GetSubWindowArea());
                    break;
                case Mode.Browser:
                default:
                    m_ManageTab.OnGUI(GetSubWindowArea());
                    break;
            }
        }

        void ModeToggle()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(kToolbarPadding);
            if (m_Mode == Mode.Browser)
            {
                bool clicked = GUILayout.Button(m_RefreshTexture);
                if (clicked)
                    m_ManageTab.ForceReloadData();
            }
            else
            {
                GUILayout.Space(m_RefreshTexture.width + kToolbarPadding);
            }
            float toolbarWidth = position.width - kToolbarPadding * 4 - m_RefreshTexture.width;
            string[] labels = new string[2] { "Configure", "Build" };
            m_Mode = (Mode)GUILayout.Toolbar((int)m_Mode, labels, "LargeButton", GUILayout.Width(toolbarWidth) );
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal(); 
        }


    }
}