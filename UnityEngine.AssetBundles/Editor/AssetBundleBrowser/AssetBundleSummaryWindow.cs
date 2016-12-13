using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System;

namespace UnityEngine.AssetBundles
{
	internal class AssetBundleSummaryWindow : EditorWindow
	{
        [SerializeField]
        TreeViewState m_treeState;
        [SerializeField]
        string m_bundlePath = string.Empty;
        public Editor editor;
        BundleTree m_tree;
        static Dictionary<string, AssetBundleCreateRequest> bundles = new Dictionary<string, AssetBundleCreateRequest>();

        internal static void ShowWindow(string bundlePath)
		{
            var window = GetWindow<AssetBundleSummaryWindow>();
            window.Init(bundlePath);
            window.Show();
		}
        
        public void OnDestroy()
        {
            editor = null;
            m_tree = null;
            List<string> toRemove = new List<string>();
            foreach (var a in bundles)
            {
                Debug.Log("Unloading bundle " + a.Key);
                if (a.Value.assetBundle != null)
                {
                    a.Value.assetBundle.Unload(true);
                    toRemove.Add(a.Key);
                }
            }
            foreach (var r in toRemove)
                bundles.Remove(r);
        }
        

        private void Init(string bundlePath)
        {
            OnDestroy();
            m_bundlePath = bundlePath;
            if (m_treeState == null)
                m_treeState = new TreeViewState();
            titleContent = new GUIContent("Asset Bundle Summary");
        }

        class BundleTree : TreeView
        {
            string m_bundlePath = string.Empty;
            AssetBundleSummaryWindow window;
            public BundleTree(AssetBundleSummaryWindow w, TreeViewState s, string p) : base(s)
            {
                window = w;
                m_bundlePath = p;
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
                AssetBundleCreateRequest req;
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
                    bundlePath = path.Replace('\\', '/');
                }

                public bool Update()
                {
                    if (!loading)
                        return false;

                    if (req == null)
                    {
                        loading = true;
                        AssetBundleCreateRequest r;
                        if (bundles.TryGetValue(bundlePath, out r))
                        {
                            Debug.Log("Unloading bundle " + bundlePath);
                            if (r.assetBundle != null)
                            {
                                Debug.Log("Found loaded bundle, unloading " + bundlePath);
                                r.assetBundle.Unload(true);
                                bundles.Remove(bundlePath);
                            }
                            else
                            {
                                Debug.Log("Found loading bundle, reusing req for " + bundlePath);
                                req = r;
                            }
                        }

                        if (req == null)
                        {
                            Debug.Log("No existing req, loading bundle " + bundlePath);
                            req = AssetBundle.LoadFromFileAsync(bundlePath);
                            bundles.Add(bundlePath, req);
                        }
                    }

                    if (req.isDone)
                    {
                        Debug.Log("Req is done for " + bundlePath + ", bundle: " + req.assetBundle);
                        displayName = Path.GetFileName(bundlePath);
                        loading = false;
                        return true;
                    }
                    else
                    {
                        int per = (int)(req.progress * 100);
                        if (per != prevPercent)
                        {
                     //       Debug.Log("Req progress update for " + bundlePath + ": " + req.progress);
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
                foreach (var f in Directory.GetFiles(m_bundlePath))
                {
                    if (Path.GetExtension(f) == ".manifest")
                    {
                        var bundleFile = f.Substring(0, f.LastIndexOf('.'));
                        if (File.Exists(bundleFile))
                            root.AddChild(new Item(bundleFile));
                    }
                }
                return root;
            }
        }

        DateTime lastUpdate;
        private void Update()
        {
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
            if (m_tree == null)
            {
                m_tree = new BundleTree(this, m_treeState, m_bundlePath);
                m_tree.Reload();
            }
            HandleHorizontalResize();
            m_tree.OnGUI(new Rect(0, 0, m_horizontalSplitterRect.x, position.height));
            if (editor != null)
            {
                GUILayout.BeginArea(new Rect(m_horizontalSplitterRect.x + splitterWidth, 0, position.width - (m_horizontalSplitterRect.x + splitterWidth), position.height));
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
        private void HandleHorizontalResize()
        {
            m_horizontalSplitterRect.x = Mathf.Clamp(m_horizontalSplitterRect.x, position.width * .1f, (position.width - splitterWidth) * .9f);
            m_horizontalSplitterRect.height = position.height;

            GUI.DrawTexture(m_horizontalSplitterRect, EditorGUIUtility.whiteTexture);
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
