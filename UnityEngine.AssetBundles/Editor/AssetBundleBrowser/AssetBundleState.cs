using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.AssetBundles
{
    public class AssetBundleState
    {
        public class BundleInfo
        {
            public string name;
            public List<AssetInfo> assets = new List<AssetInfo>();
            public int GatherImplicitDependencies(List<AssetInfo> results)
            {
                foreach (var a in assets)
                    GatherDependencies(a, results);
                return results.Count;
            }

            private void GatherDependencies(AssetInfo a, List<AssetInfo> results)
            {
                List<string> deps = new List<string>(AssetDatabase.GetDependencies(a.name, true));
                if (AssetDatabase.IsValidFolder(a.name))
                {
                    deps.AddRange(System.IO.Directory.GetFiles(a.name).Select(f => f.Replace('\\', '/')));
                    deps.AddRange(System.IO.Directory.GetDirectories(a.name).Select(f => f.Replace('\\', '/')));
                }

                foreach (var p in deps)
                {
                    var ai = GetAsset(p);
                    if (ai == null || assets.Contains(ai) || results.Contains(ai))
                        continue;

                    var b = AssetDatabase.GetImplicitAssetBundleName(ai.name);
                    if (string.IsNullOrEmpty(b) || b == name)
                    {
                        results.Add(ai);
                        GatherDependencies(ai, results);
                    }
                }
            }

            public BundleInfo(string n)
            {
                name = n;
            }

        }

        public class AssetInfo
        {
            public string name;                     
            public BundleInfo bundle;
            public AssetInfo(BundleInfo b, string n)
            {
                bundle = b;
                name = n;
            }

            internal void GatherReferences(List<AssetInfo> references)
            {
                foreach (var a in AssetDatabase.GetAllAssetPaths())
                {
                    foreach (var d in AssetDatabase.GetDependencies(a, false))
                    {
                        if (d == name)
                        {
                            var ai = GetAsset(a);
                            if(ai != null)
                                references.Add(ai);
                        }
                    }
                }
            }

            internal void GatherBundles(List<BundleInfo> results)
            {
                if (bundle.name.Length > 0)
                {
                    results.Add(bundle);
                }
                else
                {
                    var ib = AssetDatabase.GetImplicitAssetBundleName(name);
                    if (ib.Length > 0)
                    {
                        results.Add(GetBundle(ib));
                    }
                    else
                    {
                        foreach (var b in bundles)
                        {
                            if (b.Key.Length == 0)
                                continue;
                            bool added = false;
                            foreach (var a in b.Value.assets)
                            {
                                foreach (var d in AssetDatabase.GetDependencies(a.name, true))
                                {
                                    if (d == name)
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

        class AssetBundleChangeListener : AssetPostprocessor
        {
             static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                foreach (string str in importedAssets)
                     AddAsset(str);
                foreach (string str in deletedAssets)
                    RemoveAsset(str);

                for (int i = 0; i < movedAssets.Length; i++)
                {
                    RemoveAsset(movedFromAssetPaths[i]);
                    AddAsset(movedAssets[i]);
                }

                EditorWindow.GetWindow<AssetBundleBrowserWindow>().Refresh();
                dirty = true;
            }
        }

 
        public static Dictionary<string, BundleInfo> bundles = new Dictionary<string, BundleInfo>();
        public static Dictionary<string, AssetInfo> assets = new Dictionary<string, AssetInfo>();
        static bool dirty = false;

        internal static AssetInfo GetAsset(string a)
        {
            AssetInfo ai = null;
            if (assets.TryGetValue(a, out ai))
                return ai;
            return null;
        }

        public static BundleInfo GetBundle(string n)
        {
            BundleInfo i;
            if (bundles.TryGetValue(n, out i))
                return i;
            i = new BundleInfo(n);
            bundles.Add(n, i);
            return i;
        }

        public static bool CheckAndClearDirtyFlag()
        {
            if (!dirty)
                return false;
            dirty = false;
            return true;
        }

        public static void RemoveAsset(string p)
        {
            AssetInfo ai;
            if (assets.TryGetValue(p, out ai))
            {
                ai.bundle.assets.Remove(ai);
                assets.Remove(ai.name);
            }
        }

        public static AssetInfo AddAsset(string p)
        {
            if (!p.StartsWith("Assets/"))
                return null;
            string ext = System.IO.Path.GetExtension(p);
            if (ext == ".dll" || ext == ".cs" || ext == ".meta")
                return null;

            var b = GetBundle(GetBundleName(p));
            AssetInfo ai;
            if (assets.TryGetValue(p, out ai))
            {
                ai.bundle = b;
                b.assets.Remove(ai);
                b.assets.Add(ai);
                return ai;
            }
            
            ai = new AssetInfo(b, p);
            assets.Add(ai.name, ai);
            b.assets.Add(ai);
            return ai;
        }

        public static void Rebuild()
        {
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            AssetDatabase.RemoveUnusedAssetBundleNames();
            bundles.Clear();
            assets.Clear();
            foreach (var asset in AssetDatabase.GetAllAssetPaths())
                AddAsset(asset);
            EditorWindow.GetWindow<AssetBundleBrowserWindow>().Refresh();
            dirty = true;
        }

        static BundleInfo CreateBundleInfo(string n)
        {
            BundleInfo b;
            if (bundles.TryGetValue(n, out b))
                return b;
            b = new BundleInfo(n);
            bundles.Add(n, b);
            return b;
        }

        internal static string GetBundleName(string asset)
        {
            var importer = AssetImporter.GetAtPath(asset);
            var bundleName = importer.assetBundleName;
            if (importer.assetBundleVariant.Length > 0)
                bundleName = bundleName + "." + importer.assetBundleVariant;
            return bundleName;
        }

        internal static void DeleteBundle(BundleInfo bundleToRemove)
        {
            foreach (var r in bundleToRemove.assets.ToArray())
                AssetImporter.GetAtPath(r.name).SetAssetBundleNameAndVariant(string.Empty, string.Empty);
            bundles.Remove(bundleToRemove.name);
            dirty = true;
        }

        public static void MoveAssetsToBundle(BundleInfo bi, IEnumerable<AssetInfo> ais)
        {
            dirty = true;
            foreach (var a in ais)
            {
                RemoveAsset(a.name);
                if (a != null)
                    AssetImporter.GetAtPath(a.name).SetAssetBundleNameAndVariant(bi.name, string.Empty);
            }
        }

        internal static void RenameBundle(BundleInfo bi, string newName)
        {
            dirty = true;
            if (bi.assets.Count == 0)
            {
                bundles.Remove(bi.name);
                bundles.Add(bi.name = newName, bi);
            }
            else
            {
                foreach (var a in bi.assets.ToArray())
                    AssetImporter.GetAtPath(a.name).SetAssetBundleNameAndVariant(newName, string.Empty);
                bundles.Remove(bi.name);
            }
        }

        public static string editBundleName = string.Empty;
        public static BundleInfo CreateEmptyBundle(string name)
        {
            dirty = true;
            if (string.IsNullOrEmpty(name))
                name = editBundleName = "newbundle";

            BundleInfo bi = new BundleInfo(name);
            bundles.Add(bi.name, bi);
            return bi;
        }
    }
}
