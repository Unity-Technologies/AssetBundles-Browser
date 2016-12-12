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
        public interface IModification
        {
            void Apply();
			string GetDisplayString();
			IEnumerable<string> GetDisplaySubStrings();
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
        class AssetBundleChangeListener : AssetPostprocessor
        {
            public void OnPostprocessAssetbundleNameChanged(string assetPath, string previousAssetBundleName, string newAssetBundleName)
            {
                if (!ignoreExternalAssetBundleChanges)
                {
                    var bundleName = GetBundleName(assetPath);
                    if (!bundles.ContainsKey(bundleName))
                        CreateEmptyBundle(bundleName, false);
                    BundleInfo curr = bundles[bundleName];
                    MoveAssetsToBundle(curr, new AssetInfo[] { assets[assetPath] });
                    EditorWindow.GetWindow<AssetBundleBrowserWindow>().Refresh();
                }
            }

            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                foreach (string str in importedAssets)
                {
                    if (!assets.ContainsKey(str))
                    {
                        var bundleName = GetBundleName(str);
                        if (!bundles.ContainsKey(bundleName))
                            bundles.Add(bundleName, new BundleInfo(bundleName));

                        var ai = new AssetInfo(bundles[bundleName], str);
                        ai.bundle.assets.Add(ai);
                        assets.Add(str, ai);
                    }
                }
                foreach (string str in deletedAssets)
                {
					AssetInfo ai = null;
					if (assets.TryGetValue(str, out ai))
					{
						ai.bundle.assets.Remove(ai);
						assets.Remove(str);
					}
                }

                for (int i = 0; i < movedAssets.Length; i++)
                {
                    AssetInfo ai = assets[movedFromAssetPaths[i]];
                    ai.name = movedAssets[i];
                    assets.Remove(movedFromAssetPaths[i]);
                    assets.Add(ai.name, ai);

                    var bundleName = GetBundleName(ai.name);
                    BundleInfo bi = bundles[bundleName];
                    if (bi != ai.bundle)
                    {
                        ai.bundle.assets.Remove(ai);
                        ai.bundle = bi;
                        ai.bundle.assets.Add(ai);
                    }

                }
                EditorWindow.GetWindow<AssetBundleBrowserWindow>().Refresh();
            }
        }

        static bool ignoreExternalAssetBundleChanges = false;
        public const string NoBundleName = "<none>";
        public static Dictionary<string, BundleInfo> bundles = new Dictionary<string, BundleInfo>();
        public static Dictionary<string, AssetInfo> assets = new Dictionary<string, AssetInfo>();
        public static List<IModification> modifications = new List<IModification>();
        public static BundleInfo noneBundle = new BundleInfo(AssetBundleState.NoBundleName);
        static bool dirty = false;

        internal static AssetInfo GetAsset(string a)
        {
            AssetInfo ai = null;
            if (assets.TryGetValue(a, out ai))
                return ai;
            return null;
        }

        public static bool CheckAndClearDirtyFlag()
        {
            if (!dirty)
                return false;
            dirty = false;
            return true;
        }
 
        public static void Rebuild()
        {
            noneBundle = new BundleInfo(AssetBundleState.NoBundleName);
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            AssetDatabase.RemoveUnusedAssetBundleNames();
            dirty = true;
            bundles.Clear();
            assets.Clear();
            modifications.Clear();

            foreach (var asset in AssetDatabase.GetAllAssetPaths())
            {
                if (!asset.StartsWith("Assets/"))
                    continue;
                string ext = System.IO.Path.GetExtension(asset);
                if (ext != ".dll" && ext != ".cs" && !asset.StartsWith("ProjectSettings") && !asset.StartsWith("Library"))
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

        internal static string GetBundleName(string asset)
        {
            var bundleName = AssetDatabase.GetImplicitAssetBundleName(asset);
            var vn = AssetDatabase.GetImplicitAssetBundleVariantName(asset);
            if (vn.Length > 0)
                bundleName = bundleName + "." + vn;
            if (bundleName.Length == 0)
                bundleName = AssetBundleState.NoBundleName;
            return bundleName;
        }

        internal static void DeleteBundle(BundleInfo bundleToRemove)
        {
            if(bundleToRemove.assets.Count > 0)
                MoveAssetsToBundle(bundles[AssetBundleState.NoBundleName], new List<AssetInfo>(bundleToRemove.assets));
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
			public string GetDisplayString()
			{
				return "Move Assets to bundle " + bundle.name;
			}

			public IEnumerable<string> GetDisplaySubStrings()
			{
				return assetsToMove.Select(a => a.name);
			}

		}

		static void SetAssetBundle(string bundleName, IEnumerable<AssetInfo> assetsToMove)
        {
            if (bundleName == AssetBundleState.NoBundleName)
                bundleName = string.Empty;
            var variantName = string.Empty;
            int dot = bundleName.LastIndexOf('.');
            if (dot > 0)
            {
                variantName = bundleName.Substring(dot + 1);
                bundleName = bundleName.Substring(0, dot);
            }
            ignoreExternalAssetBundleChanges = true;
            foreach (var a in assetsToMove)
            {
                if (a != null)
                {
                    AssetImporter importer = AssetImporter.GetAtPath(a.name);
                    importer.SetAssetBundleNameAndVariant(bundleName, variantName);
                }
            }
            ignoreExternalAssetBundleChanges = false;
        }

        public static void MoveAssetsToBundle(BundleInfo bi, IEnumerable<AssetInfo> ais)
        {
            modifications.Add(new MoveAssetsToBundleMod(bi, ais));
            foreach (var a in ais)
            {
                if (a != null)
                {
                    a.bundle.assets.Remove(a);
                    bi.assets.Add(a);
                    a.bundle = bi;
                }
            }
            dirty = true;
        }


        public static string editBundleName = string.Empty;
        public static BundleInfo CreateEmptyBundle(string name, bool beginEdit)
        {
            dirty = true;
            BundleInfo bi = new BundleInfo(name);
            bundles.Add(bi.name, bi);
            if (beginEdit)
                editBundleName = name;
            return bi;
        }

        class RenameBundleMod : IModification
        {
            BundleInfo bundle;
			string previousName;
            public RenameBundleMod(BundleInfo bi, string prevName)
            {
				previousName = prevName;
				bundle = bi;
            }

            public void Apply()
            {
                SetAssetBundle(bundle.name, bundle.assets);
            }
			public string GetDisplayString()
			{
				return "Rename bundle '" + previousName + "' to '" + bundle.name + "'";
			}

			public IEnumerable<string> GetDisplaySubStrings()
			{
				return null;
			}
		}

        internal static void RenameBundle(BundleInfo bi, string newName)
        {
			string prevName = bi.name;
            bundles.Remove(bi.name);
            bundles.Add(bi.name = newName, bi);
            modifications.Add(new RenameBundleMod(bi, prevName));
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
