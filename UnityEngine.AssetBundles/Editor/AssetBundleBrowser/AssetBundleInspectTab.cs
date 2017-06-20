using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System;

namespace UnityEngine.AssetBundles
{
    [System.Serializable]
    public class AssetBundleInspectTab
    {
        EditorWindow m_Parent = null;
        Rect m_Position;
        [SerializeField]
        private Vector2 m_ScrollPosition;

        [SerializeField]
        private string m_BundlePath;

        private List<string> m_BundleList = new List<string>();
        private InspectBundleTree m_BundleTreeView;
        [SerializeField]
        private TreeViewState m_BundleTreeState;

        public Editor m_Editor = null;

        //private Dictionary<string, AssetBundleCreateRequest> m_BundleRequests = new Dictionary<string, AssetBundleCreateRequest>();
        //private Dictionary<string, AssetBundle> m_BundleRequests = new Dictionary<string, AssetBundle>();
        [SerializeField]
        private List<AssetBundle> m_LoadedBundles;

        //int m_AsyncDoneCount; 

        private SingleBundleInspector m_SingleInspector;


        public AssetBundleInspectTab()
        {
            m_LoadedBundles = new List<AssetBundle>();
            //m_advancedSettings = false;
            m_SingleInspector = new SingleBundleInspector();
        }

        public void SaveBundle(AssetBundle b)
        {
            m_LoadedBundles.Add(b);
        }
        public void OnEnable(Rect pos, EditorWindow parent)
        {
            m_Parent = parent;
            foreach (var bundle in m_LoadedBundles)
            {
                bundle.Unload(true);
            }
            m_LoadedBundles.Clear();

            m_Position = pos;
            if (m_BundlePath == null)
                m_BundlePath = string.Empty;

            if (m_BundleTreeState == null)
                m_BundleTreeState = new TreeViewState();
            m_BundleTreeView = new InspectBundleTree(m_BundleTreeState, this);
            m_BundleTreeView.Reload();

            if (m_BundleList == null)
                m_BundleList = new List<string>();

            RefreshBundles();

            //m_AsyncDoneCount = 0;
        }


        public void Update()
        {
            if (m_BundleTreeView.selected != null)
            {
                m_BundleTreeView.selected.Update();
            }
            //if (m_AsyncDoneCount != m_BundleRequests.Count)
            //{
            //    int doneCount = 0;
            //    foreach (var req in m_BundleRequests)
            //    {
            //        if (req.Value.isDone)
            //        {
            //            doneCount++;
            //        }
            //    }
            //    if (m_AsyncDoneCount != doneCount)
            //    {
            //        m_AsyncDoneCount = doneCount;
            //        m_BundleTreeView.Reload();
            //        Debug.Log("count has changed to: " + m_AsyncDoneCount);
            //    }
            //}
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
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            //////input path
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            var originalPath = m_BundlePath;
            m_BundlePath = EditorGUILayout.TextField("Bundle Path", m_BundlePath);

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Browse", GUILayout.MaxWidth(75f)))
                BrowseForFolder();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            if (originalPath != m_BundlePath)
            {
                Debug.Log("orig: " + originalPath + "  new: " + m_BundlePath);
                LoadBundles();
            }

            if (m_BundleList.Count > 0)
            {
                m_BundleTreeView.OnGUI(new Rect(m_Position.x, m_Position.y + 30, m_Position.width / 2.0f, m_Position.height - 30));

                if (m_BundleTreeView.selected != null)
                {
                    //m_SingleInspector.currentBundle = m_BundleTreeView.selected;
                    m_SingleInspector.OnGUI(new Rect(m_Position.x + m_Position.width / 2.0f, m_Position.y + 30, m_Position.width / 2.0f, m_Position.height - 30));
                }
                else
                {
                    GUI.Label(new Rect(m_Position.x + m_Position.width / 2.0f, m_Position.y + 30, m_Position.width / 2.0f, m_Position.height - 30), "nothing is selected");
                }

                //if (m_Editor != null)
                //{
                //    GUILayout.BeginArea(new Rect(m_Position.x + m_Position.width / 2.0f, m_Position.y + 30, m_Position.width / 2.0f, m_Position.height - 30));
                //    m_Editor.Repaint();
                //    m_Editor.OnInspectorGUI();
                //    GUILayout.EndArea();
                //}
            }

            EditorGUILayout.EndScrollView();


        }
        //TODO - this is largely copied from BuildTab, should probably be shared code.
        private void BrowseForFolder()
        {
            var newPath = EditorUtility.OpenFolderPanel("Bundle Folder", m_BundlePath, string.Empty);
            if (!string.IsNullOrEmpty(newPath))
            {
                var gamePath = System.IO.Path.GetFullPath(".");//TODO - FileUtil.GetProjectRelativePath??
                gamePath = gamePath.Replace("\\", "/");
                if (newPath.StartsWith(gamePath))
                    newPath = newPath.Remove(0, gamePath.Length + 1);
                m_BundlePath = newPath;
            }
        }
        public void RefreshBundles()
        {
            //do some prep?...
            // ...

            Debug.Log("Did someone say refresh?");
            //do some loading
            LoadBundles();
        }
        private void AddFilePathToList(string path)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetExtension(file) == ".manifest")
                {
                    var f = file.Substring(0, file.LastIndexOf('.')).Replace('\\', '/');
                    m_BundleList.Add(f);
                    //AssetBundleCreateRequest req;
                    //if (m_bundles.TryGetValue(f, out req))
                    //{
                    //    if (req.isDone && req.assetBundle != null)
                    //    {
                    //        req.assetBundle.Unload(true);
                    //        m_bundles.Remove(f);
                    //    }
                    //}
                    //if (!m_bundles.ContainsKey(f))
                    //    m_bundles.Add(f, AssetBundle.LoadFromFileAsync(f));
                }
            }

            foreach (var dir in Directory.GetDirectories(path))
            {
                AddFilePathToList(dir);
            }
        }
        private void LoadBundles()
        {
            if (m_BundlePath == string.Empty)
                return;

            Debug.Log("we aren't actually loading");
            //////////////////////////////////////
            //find assets
            if (m_BundleList == null)
                m_BundleList = new List<string>();

            m_BundleList.Clear();
            if (Directory.Exists(m_BundlePath))
            {
                AddFilePathToList(m_BundlePath);
                m_BundleTreeView.Reload();
            }

            //////////////////////////////////////
            //load assets
            //foreach (var req in m_LoadedBundles)
            //{
            //    if(req != null)
            //    {
            //        Debug.Log("Unloading bundle: " + req.name);
            //        req.Unload(true);
            //    }  
            //}
            //m_LoadedBundles.Clear();
            //m_AsyncDoneCount = 0;

            //foreach (var file in m_BundleList)
            //{ 
            //    var shortName = file.Remove(0, m_BundlePath.Length + 1);
            //    Debug.Log("Loading bundle: " + file);
            //    //AssetBundle.LoadFromFile(file);
            //    m_LoadedBundles.Add(AssetBundle.LoadFromFile(file));
            //    //AssetBundle.LoadFromFileAsync(file);
            //}
        }

        public List<string> BundleList
        { get { return m_BundleList; } }

        //public void SelectedBundle(InspectTreeItem item)
        //{
        //    Debug.Log(item.displayName);
        //    m_Editor = Editor.CreateEditor(item.bundle);
        //}

        public void SetBundleItem(InspectTreeItem selected)
        {
            m_SingleInspector.m_BundleItem = selected;
            m_SingleInspector.m_Editor = null;
            if (selected != null)
            {
                selected.LoadBundle();
                m_SingleInspector.m_Editor = Editor.CreateEditor(selected.bundle);
            }
        }

        public class InspectTreeItem : TreeViewItem
        {
            public enum State
            {
                Unselected = 0,
                Loading,
                Loaded,
                Error
            }
            public State state
            {
                get;
                private set;
            }
            private string m_BundlePath;
            private AssetBundle m_Bundle;
            public AssetBundleCreateRequest request { get; private set; }
            private AssetBundleInspectTab m_InspectTab;
            //public InspectTreeItem(int id, int depth, string displayName) : base(id, depth, displayName)
            public InspectTreeItem(string path, AssetBundleInspectTab inspectTab) : base(path.GetHashCode(), 0, path)
            {
                m_BundlePath = path;
                m_Bundle = null;
                state = State.Unselected;
                request = null;
                m_InspectTab = inspectTab;
            }
            public AssetBundle bundle
            {
                get
                {
                    return m_Bundle;
                }
            }
            public void LoadBundle()
            {
                if (m_Bundle == null)
                {
                    state = State.Loading;

                    Debug.Log("Loading bundle: " + m_BundlePath);
                    m_Bundle = AssetBundle.LoadFromFile(m_BundlePath);
                    m_InspectTab.SaveBundle(m_Bundle);
                    //request = AssetBundle.LoadFromFileAsync(m_BundlePath);

                }
                else
                    state = State.Loaded;
            }

            public void Update()
            {
                if (state == State.Loading)
                {
                    //if ( (request != null) && (request.isDone) )
                    //    state = State.Loaded;
                    if (m_Bundle != null)
                        state = State.Loaded;
                    else
                        state = State.Error;
                }
            }

        }

        class InspectBundleTree : TreeView
        {
            AssetBundleInspectTab m_InspectTab;
            //public InspectTreeItem selected { get; set; }
            public InspectBundleTree(TreeViewState s, AssetBundleInspectTab parent) : base(s)
            {
                m_InspectTab = parent;
                showBorder = true;
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem(-1, -1);
                root.children = new List<TreeViewItem>();
                if (m_InspectTab == null)
                    Debug.Log("how is m_InspectTab null???");
                else
                {
                    foreach (var b in m_InspectTab.BundleList)
                    {
                        root.AddChild(new InspectTreeItem(b, m_InspectTab));
                    }
                }
                return root;
            }

            public override void OnGUI(Rect rect)
            {
                base.OnGUI(rect);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
                {
                    SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
                }
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                base.SelectionChanged(selectedIds);
                
                if (selectedIds.Count > 0)
                {
                    m_InspectTab.SetBundleItem(FindItem(selectedIds[0], rootItem) as InspectTreeItem);
                    //if (selected != null)
                    //{
                    //    selected.LoadBundle();
                    //}
                }
                else
                    m_InspectTab.SetBundleItem(null);
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }
        }

        class SingleBundleInspector
        {
            public SingleBundleInspector() { }

            public Editor m_Editor = null;
            public InspectTreeItem m_BundleItem;

            private Rect m_Position;
            public void OnGUI(Rect pos)
            {
                if (m_BundleItem == null)
                    return;

                m_Position = pos;
                string message = "";
                switch (m_BundleItem.state)
                {
                    case InspectTreeItem.State.Loading:
                        message = "Loading bundle...";
                        //if (m_BundleTreeView.selected.request.isDone)
                        //{
                        //    m_BundleTreeView.selected.GetBundle();
                        //}
                        break;
                    case InspectTreeItem.State.Loaded:
                        DrawBundleData();
                        break;
                    case InspectTreeItem.State.Error:
                        message = "something broke";
                        break;
                    case InspectTreeItem.State.Unselected:
                        message = "the system thinks the selected item has never been selected. weird.";
                        break;
                }

                if(message != "")
                    GUI.Label(m_Position, message);
            }

            private void DrawBundleData()
            {
                //if (m_Editor == null)
                //    m_Editor = Editor.CreateEditor(currentBundle.bundle);

                GUILayout.BeginArea(m_Position);
                m_Editor.Repaint();
                m_Editor.OnInspectorGUI();
                GUILayout.EndArea();

                //string message = "";
                //var assets = currentBundle.bundle.GetAllAssetNames();
                //message = "this bundle has " + assets.Length + " asset(s) in it";
                //foreach (var s in assets)
                //{
                //    message += "\n" + s + ",";
                //}
                //if(currentBundle.bundle.mainAsset != null)
                //    message += "\n also... " + currentBundle.bundle.mainAsset.name;

                //GUI.Label(m_Position, message);
            }
        }
    }
}