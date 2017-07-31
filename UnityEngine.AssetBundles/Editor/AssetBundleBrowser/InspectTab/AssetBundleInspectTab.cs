using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace UnityEngine.AssetBundles
{
    [System.Serializable]
    public class AssetBundleInspectTab
    {
        Rect m_Position;
        [SerializeField]
        private Vector2 m_ScrollPosition;

        [SerializeField]
        private InspectTabData m_Data;
        

        private List<string> m_BundleList;
        private InspectBundleTree m_BundleTreeView;
        [SerializeField]
        private TreeViewState m_BundleTreeState;

        public Editor m_Editor = null;

        private SingleBundleInspector m_SingleInspector;

        /// <summary>
        /// Collection of loaded asset bundles indexed by path
        /// </summary>
        private Dictionary<string, AssetBundle> m_loadedAssetBundles;

        /// <summary>
        /// Returns a loaded asset bundle by path if the bundle exists in our container.
        /// </summary>
        /// <returns>Asset bundle instance if loaded, otherwise null.</returns>
        /// <param name="path">Path to the loaded asset bundle.</param>
        public AssetBundle GetLoadedBundleByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string actualPath = GetPathWithoutFileExtension(path);
            if (!m_loadedAssetBundles.ContainsKey(actualPath))
            {
                return null;
            }

            return m_loadedAssetBundles[actualPath];
        }

        /// <summary>
        /// Removes the file extension from the path if it exists.
        /// </summary>
        /// <returns>The path without file extension.</returns>
        /// <param name="path">The input path.</param>
        public static string GetPathWithoutFileExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            string fileNoExtension = Path.GetFileNameWithoutExtension(path);
            string directory = Path.GetDirectoryName(path);

            return Path.Combine(directory, fileNoExtension);
        }

        public AssetBundleInspectTab()
        {
            m_BundleList = new List<string>();
            m_SingleInspector = new SingleBundleInspector();
            m_loadedAssetBundles = new Dictionary<string, AssetBundle>();
        }

        public void OnEnable(Rect pos, EditorWindow parent)
        {
            m_Position = pos;
            if (m_Data == null)
                m_Data = new InspectTabData();

            //LoadData...
            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AssetBundleBrowserInspect.dat";

            if (File.Exists(dataPath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(dataPath, FileMode.Open);
                var data = bf.Deserialize(file) as InspectTabData;
                if (data != null)
                    m_Data = data;
                file.Close();
            }


            if (m_BundleList == null)
                m_BundleList = new List<string>(); 

            if (m_BundleTreeState == null)
                m_BundleTreeState = new TreeViewState();
            m_BundleTreeView = new InspectBundleTree(m_BundleTreeState, this);
            m_BundleTreeView.Reload();


            RefreshBundles();
            
        }

        public void OnDisable()
        {
            ClearData();

            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AssetBundleBrowserInspect.dat";

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(dataPath);

            bf.Serialize(file, m_Data);
            file.Close();
        }

        public void OnGUI(Rect pos)
        {
            m_Position = pos;

            if (Application.isPlaying)
            {
                var style = GUI.skin.label;
                style.alignment = TextAnchor.MiddleCenter;
                style.wordWrap = true;
                GUI.Label(
                    new Rect(m_Position.x + 1f, m_Position.y + 1f, m_Position.width - 2f, m_Position.height - 2f),
                    new GUIContent("Inspector unavailable while in PLAY mode"),
                    style);
            }
            else
            {
                OnGUIEditor();
            }
        }

        private void OnGUIEditor()
        {
            //////input path
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            var originalPath = m_Data.m_BundlePath;
            m_Data.m_BundlePath = EditorGUILayout.TextField("Bundle Path", m_Data.m_BundlePath);
            
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(75f)))
                BrowseForFolder();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (originalPath != m_Data.m_BundlePath)
            {
                RefreshBundles();
            }

            if (m_BundleList.Count > 0)
            {
                m_BundleTreeView.OnGUI(new Rect(m_Position.x, m_Position.y + 30, m_Position.width / 2.0f, m_Position.height - 30));
                m_SingleInspector.OnGUI(new Rect(m_Position.x + m_Position.width / 2.0f, m_Position.y + 30, m_Position.width / 2.0f, m_Position.height - 30));
            }
        }

        //TODO - this is largely copied from BuildTab, should maybe be shared code.
        private void BrowseForFolder()
        {
            var newPath = EditorUtility.OpenFolderPanel("Bundle Folder", m_Data.m_BundlePath, string.Empty);
            if (!string.IsNullOrEmpty(newPath))
            {
                var gamePath = System.IO.Path.GetFullPath(".");//TODO - FileUtil.GetProjectRelativePath??
                gamePath = gamePath.Replace("\\", "/");
                if (newPath.StartsWith(gamePath))
                    newPath = newPath.Remove(0, gamePath.Length + 1);
                m_Data.m_BundlePath = newPath;
            }
        }
        public void RefreshBundles()
        {
            ClearData();

            //Debug.Log("Did someone say refresh?");
            //do some loading
            LoadBundles();
        }
        private void ClearData()
        {
            m_SingleInspector.SetBundle(null);

            if (null != this.m_loadedAssetBundles)
            {
                List<string> paths = new List<string>(m_loadedAssetBundles.Keys);
                foreach (string path in paths)
                {
                    AssetBundle bundle = m_loadedAssetBundles[path];
                    if (null != bundle)
                    {
                        bundle.Unload(true);
                    }
                }

                m_loadedAssetBundles.Clear();
            }
        }
        private void AddFilePathToList(string path)
        {
            //////////////////////////////////////
            /// code to handle appended hash things
            //var files = Directory.GetFiles(path);
            //Array.Sort(files);
            //int size = files.Length;
            //for (int i = 0; i < size; i++)
            //{
            //    ... do something...
            //}
            //////////////////////////////////////


            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetExtension(file) == ".manifest")
                {
                    var f = file.Substring(0, file.LastIndexOf('.')).Replace('\\', '/');
                    if (File.Exists(f))
                        m_BundleList.Add(f);
                    else
                        Debug.Log("Expected bundle not found: " + f + ". Note: Browser does not yet support inspecting bundles with hash appended.");
                }
            }

            foreach (var dir in Directory.GetDirectories(path))
            {
                AddFilePathToList(dir);
            }
        }
        private void LoadBundles()
        {
            if (m_Data.m_BundlePath == string.Empty)
                return;
            
            //find assets
            if (m_BundleList == null)
                m_BundleList = new List<string>();

            m_BundleList.Clear();
            if (Directory.Exists(m_Data.m_BundlePath))
            {
                AddFilePathToList(m_Data.m_BundlePath);
            }
            m_BundleTreeView.Reload();
        }

        public List<string> BundleList
        { get { return m_BundleList; } }


        public void SetBundleItem(InspectTreeItem selected)
        {
            if (selected == null)
            {
                m_SingleInspector.SetBundle(null);
            }
            else
            {
                AssetBundle bundle = this.LoadBundle(selected.bundlePath);
                m_SingleInspector.SetBundle(bundle, selected.bundlePath);
            }
        }

        [System.Serializable]
        public class InspectTabData
        {
            public string m_BundlePath = string.Empty;
        }

        /// <summary>
        /// Loads the bundle at the specified path.
        /// Unloads previously loaded bundles if neccessary.
        /// </summary>
        /// <returns>Returns loaded bundle, null if it could not be loaded.</returns>
        /// <param name="path">Path of bundle to load</param>
        private AssetBundle LoadBundle(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            AssetBundle bundle = this.GetLoadedBundleByPath(path);
            if (null != bundle)
            {
                this.UnloadBundle(path);
            }

            string actualPath = GetPathWithoutFileExtension(path);
            bundle = AssetBundle.LoadFromFile(path);
            if (null == bundle)
            {
                return null;
            }

            m_loadedAssetBundles[actualPath] = bundle;

            string[] assetNames = bundle.GetAllAssetNames();
            foreach (string name in assetNames)
            {
                bundle.LoadAsset(name);
            }

            return bundle;
        }

        /// <summary>
        /// Unloads the bundle at the specfied path.
        /// </summary>
        /// <param name="path">Path to bundle.</param>
        private void UnloadBundle(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string actualPath = GetPathWithoutFileExtension(path);
            if (!this.m_loadedAssetBundles.ContainsKey(actualPath))
            {
                return;
            }

            AssetBundle bundle = m_loadedAssetBundles[actualPath];
            if (null == bundle)
            {
                return;
            }

            bundle.Unload(true);
            bundle = null;
            m_loadedAssetBundles.Remove(actualPath);
        }
    }
}