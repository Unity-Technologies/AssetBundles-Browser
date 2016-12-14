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
        bool m_ForceRebuild = false;
        BuildTarget buildTarget = BuildTarget.StandaloneWindows;

        [MenuItem("AssetBundles/Build", priority = 2)]
        internal static void ShowWindow()
		{
			var window = GetWindow<AssetBundleBuildWindow>();
			window.titleContent = new GUIContent("ABBuild");
			window.Show();
		}

        bool showSummary = false;
        private void Update()
        {
            if (showSummary)
            {
                AssetBundleSummaryWindow.ShowWindow(m_bundlePath);
                showSummary = false;
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

            m_ForceRebuild = GUILayout.Toggle(m_ForceRebuild, "Force Rebuild");

			if (GUILayout.Button("Build"))
			{
                if (string.IsNullOrEmpty(m_bundlePath))
                    BrowseForFolder();
                
                if (!Directory.Exists(m_bundlePath))
                {
                    Debug.Log("Invalid bundle path " + m_bundlePath);
                }
                else
                {
                    if (m_ForceRebuild)
                    {
                        if (EditorUtility.DisplayDialog("File delete confirmation", "Do you want to delete all files in the directory " + m_bundlePath + "?", "Yes", "No"))
                        {
                            Directory.Delete(m_bundlePath, true);
                            Directory.CreateDirectory(m_bundlePath);
                        }
                    }
                    BuildPipeline.BuildAssetBundles(m_bundlePath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    showSummary = true;
                }
            }
            GUILayout.EndVertical();
		}

        private void BrowseForFolder()
        {
            var f = EditorUtility.OpenFolderPanel("Bundle Folder", m_bundlePath, string.Empty);
            if (!string.IsNullOrEmpty(f))
                m_bundlePath = f;

        }
    }
}
