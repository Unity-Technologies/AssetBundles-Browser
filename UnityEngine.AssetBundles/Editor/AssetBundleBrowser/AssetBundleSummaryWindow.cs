using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System;

namespace UnityEngine.AssetBundles
{
	internal class AssetBundleSummaryWindow : EditorWindow
	{
       // TreeViewState m_treeState;
        [SerializeField]
        string m_bundlePath = string.Empty;
        public Editor editor;
        BundleTree m_tree;
        static Dictionary<string, AssetBundleCreateRequest> bundles = new Dictionary<string, AssetBundleCreateRequest>();
        [MenuItem("AssetBundles/Inspect", priority = 3)]
        static void ShowWindow()
        {
            GetWindow<AssetBundleSummaryWindow>().titleContent = new GUIContent("ABInspect");
        }

        internal static void ShowWindow(string bundlePath)
		{
            var window = GetWindow<AssetBundleSummaryWindow>();
            window.titleContent = new GUIContent("ABInspect");
            window.Init(bundlePath);
		}

        public AssetBundleSummaryWindow()
        {
            Debug.Log("AssetBundleSummaryWindow created");
        }

        private void Init(string bundlePath)
        {
            m_bundlePath = bundlePath;
        }

        class BundleTree : TreeView
        {
            AssetBundleSummaryWindow window;
            public BundleTree(AssetBundleSummaryWindow w, TreeViewState s) : base(s)
            {
                window = w;
                showBorder = true;
            }

            public bool Update()
            {
                bool updating = false;
                foreach (var i in GetRows())
                {
                    var ri = i as Item;
                    if (ri != null)
                    {
                        if (ri.Update())
                            updating = true;
                    }
                }
                return updating;
            }

            class Item : TreeViewItem
            {
                string bundlePath;
                AssetBundleCreateRequest req { get { return bundles[bundlePath]; } }
                int prevPercent = -1;
                bool loading = true;
                public Editor editor
                {
                    get
                    {
                        return (req == null || req.assetBundle == null) ? null : Editor.CreateEditor(req.assetBundle);
                    }
                }

                public Item(string path) : base(path.GetHashCode(), 0, Path.GetFileName(path))
                {
                    bundlePath = path;
                }

                public bool Update()
                {
                    if (!loading)
                        return false;
                    if (req.isDone)
                    {
                        displayName = Path.GetFileName(bundlePath);
                        loading = false;
                        return true;
                    }
                    else
                    {
                        int per = (int)(req.progress * 100);
                        if (per != prevPercent)
                        {
                            displayName = Path.GetFileName(bundlePath) + " " + (prevPercent = per) + "%";
                            return true;
                        }
                    }
                    return false;
                }
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                window.editor = (TreeViewUtility.FindItem(selectedIds[0], rootItem) as Item).editor;
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem(-1, -1);
                root.children = new List<TreeViewItem>();
                foreach (var b in bundles)
                    root.AddChild(new Item(b.Key));
                return root;
            }
        }

        [SerializeField]
        TreeViewState m_treeState;
        DateTime lastUpdate;
        private void Update()
        {
            if (m_tree == null && Directory.Exists(m_bundlePath))
            {
                foreach (var fn in Directory.GetFiles(m_bundlePath))
                {
                    if (Path.GetExtension(fn) == ".manifest")
                    {
                        var f = fn.Substring(0, fn.LastIndexOf('.')).Replace('\\', '/');
                        AssetBundleCreateRequest req;
                        if (bundles.TryGetValue(f, out req))
                        {
                            if (req.isDone && req.assetBundle != null)
                            {
                                req.assetBundle.Unload(true);
                                bundles.Remove(f);
                            }
                        }
                        if (!bundles.ContainsKey(f))
                            bundles.Add(f, AssetBundle.LoadFromFileAsync(f));
                    }
                }

               // Debug.Log("m_tree created with " + bundles.Count);
                if (m_treeState == null)
                    m_treeState = new TreeViewState();
                m_tree = new BundleTree(this, m_treeState);
                m_tree.Reload();
            }

            if (m_tree == null)
                return;


            if (m_resizingHorizontalSplitter)
                Repaint();

            if ((DateTime.Now - lastUpdate).TotalSeconds > .5f)
            {
                if (m_tree.Update())
                {
                    m_tree.SetSelection(m_tree.GetSelection());
                    Repaint();
                }
                lastUpdate = DateTime.Now;
            }
        }

        void OnGUI()
		{
            GUILayout.BeginHorizontal();
            var f = GUILayout.TextField(m_bundlePath, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Browse"))
                f = EditorUtility.OpenFolderPanel("Bundle Folder", f, string.Empty);
            if (f != m_bundlePath)
                Init(f);
            GUILayout.EndHorizontal();

            if (m_tree == null)
                return;

            float h = 21 + splitterWidth;
            HandleHorizontalResize(h);
            m_tree.OnGUI(new Rect(splitterWidth, h, m_horizontalSplitterRect.x - splitterWidth * 2, position.height - (h + splitterWidth)));
            if (editor != null)
            {
                GUILayout.BeginArea(new Rect(m_horizontalSplitterRect.x + splitterWidth, h, position.width - (m_horizontalSplitterRect.x + splitterWidth), position.height - (h + splitterWidth)));
                editor.Repaint();
                editor.OnInspectorGUI();
                GUILayout.EndArea();
            }
        }

        void OnEnable()
        {
            m_horizontalSplitterRect = new Rect(position.width / 2, 0, splitterWidth, this.position.height);
        }

        Rect m_horizontalSplitterRect;
        const float splitterWidth = 3;
        bool m_resizingHorizontalSplitter = false;
        private void HandleHorizontalResize(float h)
        {
            m_horizontalSplitterRect.x = Mathf.Clamp(m_horizontalSplitterRect.x, position.width * .1f, (position.width - splitterWidth) * .9f);
            m_horizontalSplitterRect.y = h;
            m_horizontalSplitterRect.height = position.height - (h + splitterWidth);

           // GUI.DrawTexture(m_horizontalSplitterRect, EditorGUIUtility.whiteTexture);
            EditorGUIUtility.AddCursorRect(m_horizontalSplitterRect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.mouseDown && m_horizontalSplitterRect.Contains(Event.current.mousePosition))
                m_resizingHorizontalSplitter = true;

            if (m_resizingHorizontalSplitter)
                m_horizontalSplitterRect.x = Mathf.Clamp(Event.current.mousePosition.x, position.width * .1f, (position.width - splitterWidth) * .9f);

            if (Event.current.type == EventType.MouseUp)
                m_resizingHorizontalSplitter = false;
        }

    }
}
