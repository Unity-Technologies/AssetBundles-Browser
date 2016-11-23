using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/*
 * rename bundle
 * delete bundle
 * show bundle hierarchy
 * show asset dependencies
 * show asset references
*/
namespace UnityEngine.AssetBundles
{
    public class AssetBundleState
    {
        interface IModification
        {
            void Apply();
        }

        public class BundleInfo
        {
            public string name;
            public List<AssetInfo> assets = new List<AssetInfo>();
            public BundleInfo(string n)
            {
                name = n;
            }
        }

        public class AssetInfo
        {
            public string name;                     
            public BundleInfo bundle;
            public List<string> dependencies = new List<string>();  
            public List<string> references = new List<string>();
            public AssetInfo(BundleInfo b, string n)
            {
                bundle = b;
                name = n;
            }
        }

        public static Dictionary<string, BundleInfo> bundles = new Dictionary<string, BundleInfo>();
        public static Dictionary<string, AssetInfo> assets = new Dictionary<string, AssetInfo>();
        static List<IModification> modifications = new List<IModification>();
        static bool dirty = false;
        public static bool CheckAndClearDirtyFlag()
        {
            if (!dirty)
                return false;
            dirty = false;
            return true;
        }
 
        public static void Rebuild()
        {
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            AssetDatabase.RemoveUnusedAssetBundleNames();
            dirty = true;
            bundles.Clear();
            assets.Clear();
            modifications.Clear();

            //find all bundles
            BundleInfo noneBundle = new BundleInfo("<none>");

            //find all assets
            foreach(var asset in AssetDatabase.GetAllAssetPaths())
            {
                if (!asset.StartsWith("Assets/"))
                    continue;
                string ext = System.IO.Path.GetExtension(asset);
                if (ext.Length > 0 && ext != ".dll" && ext != ".cs" && !asset.StartsWith("ProjectSettings") && !asset.StartsWith("Library"))
                {
                    AssetInfo ai = new AssetInfo(noneBundle, asset);
                    assets.Add(asset, ai);
                    noneBundle.assets.Add(ai);
                }
            }

            foreach (var b in AssetDatabase.GetAllAssetBundleNames())
            {
                BundleInfo bi = new BundleInfo(b);
                foreach (var a in AssetDatabase.GetAssetPathsFromAssetBundle(b))
                {
                    AssetInfo ai = null;
                    if (!assets.TryGetValue(a, out ai))
                    {
                        Debug.Log("Can't find asset " + a + " referenced by bundle " + b);
                        continue;
                    }
                    noneBundle.assets.Remove(ai);
                    bi.assets.Add(ai);
                    ai.bundle = bi;
                }
                bundles.Add(b, bi);
            }
            bundles.Add(noneBundle.name, noneBundle);

        }

        internal static void DeleteBundle(BundleInfo bundleToRemove)
        {
            if(bundleToRemove.assets.Count > 0)
                MoveAssetsToBundle(bundles["<none>"], new List<AssetInfo>(bundleToRemove.assets));
            bundles.Remove(bundleToRemove.name);
            dirty = true;
        }

        class MoveAssetsToBundleMod : IModification
        {
            public BundleInfo bundle;
            public IEnumerable<AssetInfo> assetsToMove;
            public MoveAssetsToBundleMod(BundleInfo b, IEnumerable<AssetInfo> a)
            {
                assetsToMove = a;
                bundle = b;
            }
            public void Apply()
            {
                SetAssetBundle(bundle.name, assetsToMove);
            }
        }

        static void SetAssetBundle(string bundleName, IEnumerable<AssetInfo> assetsToMove)
        {
            var variantName = string.Empty;
            int dot = bundleName.LastIndexOf('.');
            if (dot > 0)
            {
                variantName = bundleName.Substring(dot + 1);
                bundleName = bundleName.Substring(0, dot);
            }
            foreach (var a in assetsToMove)
            {
                AssetImporter importer = AssetImporter.GetAtPath(a.name);
                importer.SetAssetBundleNameAndVariant(bundleName, variantName);
            }
        }

        public static void MoveAssetsToBundle(BundleInfo bi, IEnumerable<AssetInfo> ais)
        {
            modifications.Add(new MoveAssetsToBundleMod(bi, ais));
            foreach (var a in ais)
            {
                a.bundle.assets.Remove(a);
                bi.assets.Add(a);
                a.bundle = bi;
            }
            dirty = true;
        }

        public static BundleInfo CreateEmptyBundle(string name)
        {
            dirty = true;
            BundleInfo bi = new BundleInfo(name);
            bundles.Add(bi.name, bi);
            return bi;
        }

        class RenameBundleMod : IModification
        {
            BundleInfo bundle;
            public RenameBundleMod(BundleInfo bi)
            {
                bundle = bi;
            }

            public void Apply()
            {
                SetAssetBundle(bundle.name, bundle.assets);
            }
        }

        internal static void RenameBundle(BundleInfo bi, string newName)
        {
            bundles.Remove(bi.name);
            bundles.Add(bi.name = newName, bi);
            modifications.Add(new RenameBundleMod(bi));
        }



        public static void ApplyChanges()
        {
            dirty = true;
            foreach (var m in modifications)
                m.Apply();
           
            ClearChanges();
        }

        public static void ClearChanges()
        {
            dirty = true;
            modifications.Clear();
            Rebuild();
        }

    }
}
