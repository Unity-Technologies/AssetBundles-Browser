using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System;

namespace UnityEngine.AssetBundles
{
	internal class AssetBundleBuildWindow : EditorWindow
	{
        [SerializeField]
        string m_bundlePath = string.Empty;
		internal static void ShowWindow()
		{
			var window = GetWindow<AssetBundleBuildWindow>();
			window.titleContent = new GUIContent("Asset Bundle Build");
			window.Show();
		}

        void OnGUI()
		{
			GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            m_bundlePath = GUILayout.TextField(m_bundlePath);
            if (GUILayout.Button("Browse"))
                BrowseForFolder();

            GUILayout.EndHorizontal();
			if (GUILayout.Button("Build"))
			{
                if (string.IsNullOrEmpty(m_bundlePath))
                    BrowseForFolder();
                AssetBundleState.ApplyChanges();
                BuildPipeline.BuildAssetBundles(m_bundlePath, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
                AssetBundleSummaryWindow.ShowWindow(m_bundlePath);
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
