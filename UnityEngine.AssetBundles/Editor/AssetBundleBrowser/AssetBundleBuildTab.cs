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
        //BuildAssetBundleOptions m_options = BuildAssetBundleOptions.None;
        //BuildAssetBundleOptions m_options = BuildAssetBundleOptions.None;
        CompressOptions m_compression = CompressOptions.StandardCompression;
        //[SerializeField]
        //bool m_ForceRebuild = false;
        //[SerializeField]
        //bool m_CopyToStreaming = false;
        //[SerializeField]
        //bool m_UseDefaultPath = true;

        [SerializeField]
        string m_outputPath = string.Empty;

        string m_streamingPath = "Assets/StreamingAssets/AssetBundles";

        class ToggleData
        {
            public ToggleData(bool s, string title, string tooltip, BuildAssetBundleOptions opt = BuildAssetBundleOptions.None)
            {
                state = s;
                content = new GUIContent(title, tooltip);
                option = opt;
            }
            public bool state;
            public GUIContent content;
            public BuildAssetBundleOptions option;
        }
        List<ToggleData> m_toggleData;
        ToggleData m_ForceRebuild;
        ToggleData m_CopyToStreaming;
        ToggleData m_UseDefaultPath;
        GUIContent m_TargetContent;
        GUIContent m_CompressionContent;


        public void OnEnable(Rect pos, EditorWindow parent)
        {
            m_toggleData = new List<ToggleData>();
            //m_toggleData.Add(new ToggleData(
            //    false,
            //    "Uncompress Asset Bundle",
            //    "Don't compress the data when creating the asset bundle.",
            //    BuildAssetBundleOptions.UncompressedAssetBundle));
            m_toggleData.Add(new ToggleData(
                false,
                "Disable Write Type Tree",
                "Do not include type information within the asset bundle",
                BuildAssetBundleOptions.DisableWriteTypeTree));
            m_toggleData.Add(new ToggleData(
                false,
                "Deterministic AssetBundle",
                "Builds an asset bundle using a hash for the id of the object stored in the asset bundle",
                BuildAssetBundleOptions.DeterministicAssetBundle));
            m_toggleData.Add(new ToggleData(
                false,
                "ForceRebuild AssetBundle",
                "Force rebuild the asset bundles",
                BuildAssetBundleOptions.ForceRebuildAssetBundle));
            m_toggleData.Add(new ToggleData(
                false,
                "Ignore Type Tree Changes",
                "Ignore the type tree changes when doing the incremental build check.",
                BuildAssetBundleOptions.IgnoreTypeTreeChanges));
            m_toggleData.Add(new ToggleData(
                false,
                "Append Hash To AssetBundle Name",
                "Append the hash to the assetBundle name.",
                BuildAssetBundleOptions.AppendHashToAssetBundleName));
            //m_toggleData.Add(new ToggleData(
            //    false,
            //    "Chunk Based Compression (LZ4)",
            //    "Use chunk-based LZ4 compression when creating the AssetBundle.",
            //    BuildAssetBundleOptions.ChunkBasedCompression));
            m_toggleData.Add(new ToggleData(
                false,
                "Strict Mode",
                "Do not allow the build to succeed if any errors are reporting during it.",
                BuildAssetBundleOptions.StrictMode));
            m_toggleData.Add(new ToggleData(
                false,
                "Dry Run Build",
                "Do a dry run build.",
                BuildAssetBundleOptions.DryRunBuild));


            m_ForceRebuild = new ToggleData(
                false,
                "Clear all build folders on build",
                "Will wipe out all contents of build directory as well as StreamingAssets/AssetBundles if you are choosing to copy build there.");
            m_CopyToStreaming = new ToggleData(
                false,
                "Copy bundles to " + m_streamingPath,
                "After build completes, will copy all build content to " + m_streamingPath + " for use in stand-alone player.");

            m_UseDefaultPath = new ToggleData(
                false,
                "Use default output directory",
                "Allows setting or browsing to custom output directory.");

            m_TargetContent = new GUIContent("Target", "Choose target platform to build for.");
            m_CompressionContent = new GUIContent("Compression", "Choose no compress, standard (LZMA), or chunk based (LZ4)");
        }


        public void Update()
        {
        }

        public void OnGUI(Rect pos)
        {
            //options
            EditorGUILayout.Space();
            GUILayout.BeginVertical();
            m_buildTarget = (ValidBuildTarget)EditorGUILayout.EnumPopup(m_TargetContent, m_buildTarget);
            m_compression = (CompressOptions)EditorGUILayout.EnumPopup(m_CompressionContent, m_compression);
            //m_options = (BuildAssetBundleOptions)EditorGUILayout.EnumMaskPopup("Options", m_options);
            EditorGUILayout.Space();
            foreach (var tog in m_toggleData)
            {
                tog.state = GUILayout.Toggle(
                    tog.state,
                    tog.content);
            }
            EditorGUILayout.Space();
            m_ForceRebuild.state = GUILayout.Toggle(
                m_ForceRebuild.state,
                m_ForceRebuild.content);
            m_CopyToStreaming.state = GUILayout.Toggle(
                m_CopyToStreaming.state,
                m_CopyToStreaming.content);

            //output path
            EditorGUILayout.Space();
            m_UseDefaultPath.state = GUILayout.Toggle(
                m_UseDefaultPath.state,
                m_UseDefaultPath.content);
            GUILayout.BeginHorizontal();
            if(string.IsNullOrEmpty(m_outputPath))
                m_outputPath = EditorUserBuildSettings.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath");

            var origPath = m_outputPath;
            if (m_UseDefaultPath.state)
            {
                m_outputPath = "AssetBundles/";
                m_outputPath += m_buildTarget.ToString();
                GUILayout.Label("Output Directory:  ");
                GUILayout.Label(m_outputPath);
            }
            else
            {
                GUILayout.Label("Output Directory:  ");
                if (GUILayout.Button("Browse"))
                    BrowseForFolder();
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

            if (string.IsNullOrEmpty(m_outputPath)) //in case they hit "cancel" on the open browser
            {
                Debug.LogError("AssetBundle Build: No valid output path for build.");
                return;
            }

            if (m_ForceRebuild.state)
            {
                string message = "Do you want to delete all files in the directory " + m_outputPath;
                if (m_CopyToStreaming.state)
                    message += " and " + m_streamingPath;
                message += "?";
                if (EditorUtility.DisplayDialog("File delete confirmation", message, "Yes", "No"))
                {
                    try
                    {
                        if (Directory.Exists(m_outputPath))
                            Directory.Delete(m_outputPath, true);

                        if (m_CopyToStreaming.state)
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
            BuildAssetBundleOptions opt = BuildAssetBundleOptions.None;
            if (m_compression == CompressOptions.Uncompressed)
                opt |= BuildAssetBundleOptions.UncompressedAssetBundle;
            else if (m_compression == CompressOptions.ChunkBasedCompression)
                opt |= BuildAssetBundleOptions.ChunkBasedCompression;
            foreach (var tog in m_toggleData)
            {
                if (tog.state)
                    opt |= tog.option;
            }
            BuildPipeline.BuildAssetBundles(m_outputPath, opt, (BuildTarget)m_buildTarget);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            if(m_CopyToStreaming.state)
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
            {
                var gamePath = System.IO.Path.GetFullPath(".");
                gamePath = gamePath.Replace("\\", "/");
                if (newPath.StartsWith(gamePath))
                    newPath = newPath.Remove(0, gamePath.Length+1);
                EditorUserBuildSettings.SetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath", m_outputPath = newPath);
            }
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
        public enum CompressOptions
        {
            Uncompressed = 0,
            StandardCompression,
            ChunkBasedCompression,
        }
    }
}