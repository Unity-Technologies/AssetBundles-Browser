using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.AssetBundles
{

    public class AssetBundleBrowserMain : EditorWindow, IHasCustomMenu
    {

        public const float kButtonWidth = 150;

        enum Mode
        {
            Browser,
            Builder,
            //Inspect,
        }
        [SerializeField]
        Mode m_Mode;

        [SerializeField]
        public AssetBundleManageTab m_ManageTab;

        [SerializeField]
        public AssetBundleBuildTab m_BuildTab;

        //[SerializeField]
        //public AssetBundleInspectTab m_InspectTab;

        private Texture2D m_RefreshTexture;

        const float k_ToolbarPadding = 15;
        const float k_MenubarPadding = 32;

        [MenuItem("Window/AssetBundle Browser", priority = 2050)]
        static void ShowWindow()
        {
            var window = GetWindow<AssetBundleBrowserMain>();
            window.titleContent = new GUIContent("AssetBundles");
            window.Show();
        }

        [SerializeField]
        public bool multiOperation = false;
        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            //menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Custom Sources"), multiOperation, FlipOperation);
        }
        public void FlipOperation()
        {
            multiOperation = !multiOperation;
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

        private Rect GetSubWindowArea()
        {
            float padding = k_MenubarPadding;
            if (multiOperation)
                padding += k_MenubarPadding * 0.5f;
            Rect subPos = new Rect(0, padding, position.width, position.height - padding);
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
            GUILayout.Space(k_ToolbarPadding);
            if (m_Mode == Mode.Browser)
            {
                bool clicked = GUILayout.Button(m_RefreshTexture);
                if (clicked)
                    m_ManageTab.ForceReloadData();
            }
            else
            {
                GUILayout.Space(m_RefreshTexture.width + k_ToolbarPadding);
            }
            float toolbarWidth = position.width - k_ToolbarPadding * 4 - m_RefreshTexture.width;
            string[] labels = new string[2] { "Configure", "Build" };
            m_Mode = (Mode)GUILayout.Toolbar((int)m_Mode, labels, "LargeButton", GUILayout.Width(toolbarWidth) );
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if(multiOperation)
            {
                //GUILayout.BeginArea(r);
                GUILayout.BeginHorizontal();

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("Bundle Data Source:");
                    GUILayout.FlexibleSpace();
                    var c = new GUIContent(string.Format("{0} ({1})", AssetBundleModel.Model.Operation.Name, AssetBundleModel.Model.Operation.ProviderName), "Select Asset Bundle Set");
                    if (GUILayout.Button(c , EditorStyles.toolbarPopup) )
                    {
                        GenericMenu menu = new GenericMenu();
                        bool firstItem = true;

                        foreach (var info in AssetBundleOperation.ABOperationProviderUtility.CustomABOperationProviderTypes)
                        {
                            var newProvider = info.CreateInstance();

                            if (!firstItem)
                            {
                                menu.AddSeparator("");
                            }

                            for (int i = 0; i < newProvider.GetABOperationCount(); ++i)
                            {
                                var op = newProvider.CreateOperation(i);

                                menu.AddItem(new GUIContent(string.Format("{0} ({1})", op.Name, op.ProviderName)), false,
                                    () =>
                                    {
                                        var thisOperation = op;
                                        AssetBundleModel.Model.Operation = thisOperation;
                                        m_ManageTab.ForceReloadData();
                                    }
                                );
                            }

                            firstItem = false;
                        }

                        menu.DropDown(new Rect(4f, 8f, 0f, 0f));
                    }

                    GUILayout.FlexibleSpace();
                    if (AssetBundleModel.Model.Operation.IsReadOnly())
                    {
                        GUIStyle tbLabel = new GUIStyle(EditorStyles.toolbar);
                        tbLabel.alignment = TextAnchor.MiddleRight;

                        GUILayout.Label("Read Only", tbLabel);
                    }
                }

                GUILayout.EndHorizontal();
                //GUILayout.EndArea();
            }
        }


    }
}