using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;

namespace UnityEngine.AssetBundles
{
	internal class AssetBundleBuildWindow : EditorWindow
	{
        [SerializeField]
        string m_bundlePath = string.Empty;
        bool m_showSummary = false;
        bool m_ForceRebuild = false;
        BuildTarget m_buildTarget = BuildTarget.StandaloneWindows;
        BuildAssetBundleOptions m_options = BuildAssetBundleOptions.None;

        [MenuItem("AssetBundles/Build", priority = 2)]
        internal static void ShowWindow()
		{
			var window = GetWindow<AssetBundleBuildWindow>();
			window.titleContent = new GUIContent("ABBuild");
			window.Show();
		}
        [MenuItem("AssetBundles/Create Test Assets", priority = 12)]
        static void CreateTestAssets()
        {
            int count = 10;
            for (int i = 0; i < count; i++)
            {
                GameObject o = new GameObject("GO_"+ i);
                o.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Transparent/Diffuse"));
                o.AddComponent<MeshRenderer>().sharedMaterial.name = "MAT" + i;
                PrefabUtility.CreatePrefab("Assets/" + o.name, o);                
            }
        }
        private void Update()
        {
            if (m_showSummary)
            {
                AssetBundleSummaryWindow.ShowWindow(m_bundlePath);
                m_showSummary = false;
            }
        }

        void OnGUI()
		{
			GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            m_bundlePath = GUILayout.TextField(m_bundlePath);
            if (GUILayout.Button("Browse"))
                BrowseForFolder();
            GUILayout.EndHorizontal();

            m_ForceRebuild = GUILayout.Toggle(m_ForceRebuild, "Force Rebuild/Clear Bundle Folder");
            m_buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Target", m_buildTarget);
            m_options = (BuildAssetBundleOptions)EditorGUILayout.EnumMaskPopup("Options", m_options);

            if (GUILayout.Button("Build"))
			{
                if (string.IsNullOrEmpty(m_bundlePath))
                    BrowseForFolder();

                if (m_ForceRebuild)
                {
                    if (EditorUtility.DisplayDialog("File delete confirmation", "Do you want to delete all files in the directory " + m_bundlePath + "?", "Yes", "No"))
                    {
                        try
                        {
                            if(Directory.Exists(m_bundlePath))
                                Directory.Delete(m_bundlePath, true);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
                if (!Directory.Exists(m_bundlePath))
                    Directory.CreateDirectory(m_bundlePath);
                BuildPipeline.BuildAssetBundles(m_bundlePath, m_options, m_buildTarget);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                // m_showSummary = true;
            }
            GUILayout.EndVertical();
		}

        private void BrowseForFolder()
        {
            var newPath = EditorUtility.OpenFolderPanel("Bundle Folder", m_bundlePath, string.Empty);
            if (!string.IsNullOrEmpty(newPath))
                m_bundlePath = newPath;
        }
    }
}
