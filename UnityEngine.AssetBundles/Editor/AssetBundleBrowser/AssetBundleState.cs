using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.AssetBundles
{
    public class AssetBundleState
    {
        public class BundleInfo
        {
            public class TreeItem : TreeViewItem
            {
                public BundleInfo bundle;
                public TreeItem() : base(-1, -1) { }
                public TreeItem(int i, int d, string n) : base(i, d, n)
                {
                    icon = Utilities.FoldlerIcon;
                }
                public TreeItem(BundleInfo b, int depth) : base(b.m_name.GetHashCode(), depth, b.DisplayName)
                {
                    bundle = b;
                    icon = Utilities.FoldlerIcon;
                }

                internal string GetPath()
                {
                    var n = displayName;
                    TreeViewItem p = parent;
                    while (p != null && !string.IsNullOrEmpty(p.displayName))
                    {
                        n = p.displayName + "/" + n;
                        p = p.parent;
                    }
                    return n;
                }
            }

            public string m_name;
            public Dictionary<string, AssetInfo> m_assets = new Dictionary<string, AssetInfo>();
            public int GatherImplicitDependencies(List<AssetInfo> results)
            {
                foreach (var a in m_assets)
                    GatherDependencies(a.Value, results);
                return results.Count;
            }

            public void GatherDependencies(AssetInfo a, List<AssetInfo> results)
            {
                foreach (var ai in a.dependencies)
                {
                    if (ai == a || m_assets.ContainsKey(ai.m_name) || results.Contains(ai))
                        continue;

                    var b = AssetDatabase.GetImplicitAssetBundleName(ai.m_name);
                    if (string.IsNullOrEmpty(b) || b == m_name)
                        results.Add(ai);
                }
            }

            public BundleInfo(string n)
            {
                m_name = n;
            }

            public string DisplayName
            {
                get
                {
                    if (!m_name.Contains('/'))
                        return m_name;
                    return m_name.Substring(m_name.LastIndexOf('/') + 1);
                }
            }

            public string Folder
            {
                get
                {
                    if (!m_name.Contains('/'))
                        return string.Empty;
                    return m_name.Substring(0, m_name.LastIndexOf('/'));
                }
            }

        }

        public class AssetInfo
        {
            public class TreeItem : TreeViewItem
            {
                public AssetInfo asset;
                public Color color;
                public TreeItem() : base(-1, -1) { color = Color.white; }
                public TreeItem(AssetInfo ai, int depth, Color c) : base(ai.m_name.GetHashCode(), depth, System.IO.Path.GetFileNameWithoutExtension(ai.m_name))
                {
                    asset = ai;
                    color = c;
                    icon = AssetDatabase.GetCachedIcon(asset.m_name) as Texture2D;
                }
                public TreeItem(AssetInfo ai, int depth, string dn) : base(ai.m_name.GetHashCode(), depth, dn)
                {
                    asset = ai;
                    color = Color.white;
                    icon = AssetDatabase.GetCachedIcon(asset.m_name) as Texture2D;
                }
            }

            public string m_name;                     
            public BundleInfo m_bundle;
            List<AssetInfo> _dependencies = null;
            public List<AssetInfo> dependencies
            {
                get
                {
                    if (_dependencies == null)
                    {
                        _dependencies = new List<AssetInfo>();
                        if (AssetDatabase.IsValidFolder(m_name))
                        {
                            GatherFoldersAndFiles(m_name, _dependencies);
                        }
                        else
                        {
                            foreach (var d in AssetDatabase.GetDependencies(m_name, true))
                            {
                                if (d != m_name && d.StartsWith("Assets/"))
                                {
                                    string ext = System.IO.Path.GetExtension(d);
                                    if (ext == ".cs" || ext == ".dll" || ext == ".js" || ext == ".boo")
                                        continue;
                                    var a = GetAsset(d);
                                    if(a != null)
                                        _dependencies.Add(a);
                                }
                            }
                        }
                    }
                    return _dependencies;
                }
            }

            internal string GetSizeString()
            {
                return "100kb";
            }

            public AssetInfo(BundleInfo b, string n)
            {
                m_bundle = b;
                m_name = n;
            }

            private void GatherFoldersAndFiles(string a, List<AssetInfo> results)
            {
                foreach (var f in System.IO.Directory.GetFiles(a))
                {
                    string ext = System.IO.Path.GetExtension(f);
                    if (ext == ".cs" || ext == ".dll" || ext == ".js" || ext == ".boo")
                        continue;
                    var ai = GetAsset(f.Replace('\\', '/'));
                    if(ai != null)
                        results.Add(ai);
                }

                foreach (var f in System.IO.Directory.GetDirectories(a))
                {
                    string path = f.Replace('\\', '/');
                    var ai = GetAsset(path);
                    if (ai != null)
                    {
                        results.Add(ai);
                        GatherFoldersAndFiles(path, results);
                    }
                }
            }


            internal void GatherReferences(List<AssetInfo> references)
            {
                foreach (var a in m_bundle.m_assets)
                {
                    if (a.Value.dependencies.Contains(this))
                        references.Add(a.Value);
                }
            }

            internal void GatherBundles(List<BundleInfo> results)
            {
                if (m_bundle.m_name.Length > 0)
                {
                    results.Add(m_bundle);
                }
                else
                {
                    var ib = AssetDatabase.GetImplicitAssetBundleName(m_name);
                    if (ib.Length > 0)
                    {
                        results.Add(GetBundle(ib));
                    }
                    else
                    {
                        foreach (var b in m_bundles)
                        {
                            if (b.Key.Length == 0)
                                continue;
                            bool added = false;
                            foreach (var a in b.Value.m_assets)
                            {
                                foreach (var d in a.Value.dependencies)
                                {
                                    if (d.m_name == m_name)
                                    {
                                        results.Add(b.Value);
                                        added = true;
                                        break;
                                    }
                                }
                                if (added)
                                    break;
                            }
                        }
                    }
                }
            }
        }

        internal static void RemoveBundle(string v)
        {
            m_bundles.Remove(v);
            AssetDatabase.RemoveAssetBundleName(v, true);
        }

        static List<string> m_importedAssets = new List<string>();
        static List<string> m_deletedAssets = new List<string>();
        static List<KeyValuePair<string, string>> m_movedAssets = new List<KeyValuePair<string, string>>();

        class AssetBundleChangeListener : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                m_importedAssets.AddRange(importedAssets);
                m_deletedAssets.AddRange(deletedAssets);
                for (int i = 0; i < movedAssets.Length; i++)
                    m_movedAssets.Add(new KeyValuePair<string, string>(movedFromAssetPaths[i], movedAssets[i]));
                m_dirty = true;
            }
        }

 
        public static Dictionary<string, BundleInfo> m_bundles = new Dictionary<string, BundleInfo>();
        static Dictionary<string, AssetInfo> m_assets = new Dictionary<string, AssetInfo>();
        public static string m_editBundleName = string.Empty;
        static bool m_dirty = false;

        public static bool CheckAndClearDirtyFlag()
        {
            if (!m_dirty)
                return false;

            foreach (var str in m_importedAssets)
                GetAsset(str);
            foreach (var str in m_deletedAssets)
                RemoveAsset(str);
            foreach (var a in m_movedAssets)
            {
                RemoveAsset(a.Key);
                GetAsset(a.Value);
            }
            m_importedAssets.Clear();
            m_deletedAssets.Clear();
            m_movedAssets.Clear();

            m_dirty = false;
            return true;
        }

        public static void RemoveAsset(string p)
        {
            AssetInfo ai;
            if (m_assets.TryGetValue(p, out ai))
            {
                ai.m_bundle.m_assets.Remove(ai.m_name);
                m_assets.Remove(ai.m_name);
                m_dirty = true;
            }
        }

        public static AssetInfo GetAsset(string p)
        {
            if (!p.StartsWith("Assets/"))
                return null;
            string ext = System.IO.Path.GetExtension(p);
            if (ext == ".dll" || ext == ".cs" || ext == ".meta")
                return null;

            AssetInfo ai;
            var b = GetBundle(GetBundleName(p));
            if (m_assets.TryGetValue(p, out ai))
            {
                if (ai.m_bundle != b)
                {
                    ai.m_bundle.m_assets.Remove(ai.m_name);
                    ai.m_bundle = b;
                    b.m_assets.Add(ai.m_name, ai);
                }
                return ai;
            }

            ai = new AssetInfo(b, p);
            m_assets.Add(ai.m_name, ai);
            b.m_assets.Add(ai.m_name, ai);
            m_dirty = true;
            return ai;
        }

        public static BundleInfo GetBundle(string n)
        {
            if (n == null)
                n = m_editBundleName = "newbundle";

            BundleInfo i;
            if (m_bundles.TryGetValue(n, out i))
                return i;
            i = new BundleInfo(n);
            m_bundles.Add(n, i);
            m_dirty = true;
            return i;
        }

        static string[] allAssets = null;
        static int allAssetsIndex = 0;
        public static void Rebuild()
        {
            AssetDatabase.RemoveUnusedAssetBundleNames();
            m_bundles.Clear();
            m_assets.Clear();

            foreach (var b in AssetDatabase.GetAllAssetBundleNames())
                foreach (var a in AssetDatabase.GetAssetPathsFromAssetBundle(b))
                    GetAsset(a);
            allAssets = AssetDatabase.GetAllAssetPaths();
            allAssetsIndex = 0;
            m_dirty = true;
        }

        internal static void Update()
        {
            if (allAssets != null)
            {
                bool dirty = m_dirty;
                GetAsset(allAssets[allAssetsIndex++]);
                m_dirty = dirty;
                if (allAssetsIndex >= allAssets.Length)
                    allAssets = null;
            }
        }

        internal static string GetBundleName(string asset)
        {
            var importer = AssetImporter.GetAtPath(asset);
            if (importer == null)
                return string.Empty;
            var bundleName = importer.assetBundleName;
            if (importer.assetBundleVariant.Length > 0)
                bundleName = bundleName + "." + importer.assetBundleVariant;
            return bundleName;
        }

        internal static void RenameBundle(BundleInfo bi, string newName)
        {
            MoveAssetsToBundle(bi.m_assets.Values, newName);
            m_bundles.Remove(bi.m_name);
            m_dirty = true;
        }

        class ABMove
        {
            public string asset;
            public string bundle;
            public ABMove(string a, string b)
            {
                asset = a;
                bundle = b;
            }
            public void Apply()
            {
                AssetImporter.GetAtPath(asset).SetAssetBundleNameAndVariant(bundle, string.Empty);
            }
        }

        static List<ABMove> moveBatches = null;

        public static void StartABMoveBatch()
        {
            if (moveBatches == null)
                moveBatches = new List<ABMove>();
        }

        public static void EndABMoveBatch()
        {
            if (moveBatches == null)
                return;
            bool autoRefresh = EditorPrefs.GetBool("kAutoRefresh");
            EditorPrefs.SetBool("kAutoRefresh", false);
            EditorUtility.DisplayProgressBar("Moving assets to bundles", "", 0);
            //foreach (var m in moveBatches)
            for (int i = 0; i < moveBatches.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Moving assets to bundle " + moveBatches[i].bundle, System.IO.Path.GetFileNameWithoutExtension(moveBatches[i].asset), (float)i / (float)moveBatches.Count);
                moveBatches[i].Apply();
            }
            EditorUtility.ClearProgressBar();
            EditorPrefs.SetBool("kAutoRefresh", autoRefresh);
            moveBatches = null;
            m_dirty = true;
        }

        public static void MoveAssetsToBundle(IEnumerable<AssetInfo> ais, string bundleName)
        {
            var bi = GetBundle(bundleName);
            List<AssetInfo> toMove = new List<AssetInfo>(ais);
            bool autoRefresh = EditorPrefs.GetBool("kAutoRefresh");
            EditorPrefs.SetBool("kAutoRefresh", false);
            foreach (var a in ais)
            {
                if (a != null && a.m_bundle != bi)
                {
                    if (moveBatches == null)
                        AssetImporter.GetAtPath(a.m_name).SetAssetBundleNameAndVariant(bi.m_name, string.Empty);
                    else
                        moveBatches.Add(new ABMove(a.m_name, bi.m_name));
                }
            }
            EditorPrefs.SetBool("kAutoRefresh", autoRefresh);
            m_dirty = true;
        }

        class MoveToBundleData
        {
            public IEnumerable<AssetInfo> paths;
            public string bundle;
            public MoveToBundleData(string b, IEnumerable<AssetInfo> p)
            {
                bundle = b;
                paths = p;
            }
        }

        public static void ShowAssetContextMenu(IEnumerable<AssetInfo> targets)
        {
            foreach (var t in targets)
                if(t == null)
                    return;
            GenericMenu menu = new GenericMenu();
            foreach (var b in AssetBundleState.m_bundles)
            {
                menu.AddItem(new GUIContent("Move to bundle/" + (b.Key.Length == 0 ? "None" : b.Key)), false, MoveToBundle, new MoveToBundleData(b.Key, targets));
            }
            menu.AddItem(new GUIContent("Move to bundle/<Create New Bundle...>"), false, MoveToBundle, new MoveToBundleData(null, targets));
            menu.ShowAsContext();
        }

        static void MoveToBundle(object target)
        {
            var bi = target as MoveToBundleData;
            StartABMoveBatch();
            MoveAssetsToBundle(bi.paths, bi.bundle);
            EndABMoveBatch();
        }
    }
}
