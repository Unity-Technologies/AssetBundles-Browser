using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;


namespace UnityEngine.AssetBundles
{
    [System.Serializable]
    public class AssetBundleBuildTab
    {
        // gui vars
        [SerializeField]
        ValidBuildTarget m_buildTarget = ValidBuildTarget.StandaloneWindows;
        [SerializeField]
        BuildAssetBundleOptions m_options = BuildAssetBundleOptions.None;
        [SerializeField]
        bool m_ForceRebuild = false;
        [SerializeField]
        bool m_CopyToStreaming = false;
        [SerializeField]
        bool m_UseDefaultPath = true;

        [SerializeField]
        string m_outputPath = string.Empty;
        string m_streamingPath = "Assets/StreamingAssets/AssetBundles";

        public void OnEnable(Rect pos, EditorWindow parent)
        {

        }


        public void Update()
        {
        }

        public void OnGUI(Rect pos)
        {
            //options
            EditorGUILayout.Space();
            GUILayout.BeginVertical();
            m_buildTarget = (ValidBuildTarget)EditorGUILayout.EnumPopup("Target", m_buildTarget);
            m_options = (BuildAssetBundleOptions)EditorGUILayout.EnumMaskPopup("Options", m_options);
            m_ForceRebuild = GUILayout.Toggle(m_ForceRebuild, "Clear all build folders on build");
            m_CopyToStreaming = GUILayout.Toggle(m_CopyToStreaming, "Copy bundles to " + m_streamingPath);


            //output path
            EditorGUILayout.Space();
            m_UseDefaultPath = GUILayout.Toggle(m_UseDefaultPath, "Use default output directory.");
            GUILayout.BeginHorizontal();
            if(string.IsNullOrEmpty(m_outputPath))
                m_outputPath = EditorUserBuildSettings.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath");

            var origPath = m_outputPath;
            if (m_UseDefaultPath)
            {
                m_outputPath = "AssetBundles/";
                m_outputPath += m_buildTarget.ToString();
                GUILayout.Label("Output Directory:  ");
                GUILayout.Label(m_outputPath);
            }
            else
            {
                GUILayout.Label("Output Directory:  ");
                m_outputPath = GUILayout.TextArea(m_outputPath);
            }
            if(m_outputPath != origPath)
            {
                EditorUserBuildSettings.SetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath", m_outputPath);
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();



            // build.
            EditorGUILayout.Space();
            if (GUILayout.Button("Build") )
            {
                ExecuteBuild();
            }
            GUILayout.EndVertical();
            
           
        }

        private void ExecuteBuild()
        {
            if (string.IsNullOrEmpty(m_outputPath))
                BrowseForFolder();

            if (m_ForceRebuild)
            {
                string message = "Do you want to delete all files in the directory " + m_outputPath;
                if (m_CopyToStreaming)
                    message += " and " + m_streamingPath;
                message += "?";
                if (EditorUtility.DisplayDialog("File delete confirmation", message, "Yes", "No"))
                {
                    try
                    {
                        if (Directory.Exists(m_outputPath))
                            Directory.Delete(m_outputPath, true);

                        if (m_CopyToStreaming)
                            if (Directory.Exists(m_streamingPath))
                                Directory.Delete(m_streamingPath, true);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            if (!Directory.Exists(m_outputPath))
                Directory.CreateDirectory(m_outputPath);
            BuildPipeline.BuildAssetBundles(m_outputPath, m_options, (BuildTarget)m_buildTarget);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            if(m_CopyToStreaming)
                DirectoryCopy(m_outputPath, m_streamingPath);
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }


            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            }
            
        }
        private void BrowseForFolder()
        {
            var newPath = EditorUtility.OpenFolderPanel("Bundle Folder", m_outputPath, string.Empty);
            if (!string.IsNullOrEmpty(newPath))
                EditorUserBuildSettings.SetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath", m_outputPath = newPath);
        }

        //Note: this is the provided BuildTarget enum with some entries removed as they are invalid in the dropdown
        public enum ValidBuildTarget
        {
            //NoTarget = -2,
            //iPhone = -1,
            //BB10 = -1,
            //MetroPlayer = -1,
            StandaloneOSXUniversal = 2,
            StandaloneOSXIntel = 4,
            StandaloneWindows = 5,
            WebPlayer = 6,
            WebPlayerStreamed = 7,
            iOS = 9,
            PS3 = 10,
            XBOX360 = 11,
            Android = 13,
            StandaloneLinux = 17,
            StandaloneWindows64 = 19,
            WebGL = 20,
            WSAPlayer = 21,
            StandaloneLinux64 = 24,
            StandaloneLinuxUniversal = 25,
            WP8Player = 26,
            StandaloneOSXIntel64 = 27,
            BlackBerry = 28,
            Tizen = 29,
            PSP2 = 30,
            PS4 = 31,
            PSM = 32,
            XboxOne = 33,
            SamsungTV = 34,
            N3DS = 35,
            WiiU = 36,
            tvOS = 37,
            Switch = 38
        }
    }
}