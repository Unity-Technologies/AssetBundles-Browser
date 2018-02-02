using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBrowser
{
    [Serializable]
    public struct AssetBundleConfiguration
    {
        public AssetBundleEntry[] assetBundles;
    }

    [Serializable]
    public struct AssetBundleEntry
    {
        public string assetBundleName;
        public string assetBundleVariant;
        public string[] assetPaths;
    }

    static class ExportImport
    {
        static AssetBundleConfiguration GetAssetBundleConfiguration()
        {
            var config = new AssetBundleConfiguration();
            string[] bundles = AssetDatabase.GetAllAssetBundleNames();
            config.assetBundles = new AssetBundleEntry[bundles.Length];
            for (int i = 0; i < bundles.Length; i++)
            {
                config.assetBundles[i].assetBundleName = bundles[i];
                config.assetBundles[i].assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundles[i]);
                if (bundles[i].Contains("."))
                {
                    // Since asset bundle can contain "." and not be a variant, we need more information
                    string variant = AssetDatabase.GetImplicitAssetBundleVariantName(config.assetBundles[i].assetPaths[0]);
                    config.assetBundles[i].assetBundleVariant = variant;
                    if (!string.IsNullOrEmpty(variant))
                        config.assetBundles[i].assetBundleName = bundles[i].Substring(0, bundles[i].Length - (variant.Length + 1));
                }
            }
            return config;
        }

        public static void ExportToJson(string path)
        {
            AssetBundleConfiguration config = GetAssetBundleConfiguration();
            string json = JsonUtility.ToJson(config, true);
            File.WriteAllText(path, json);
        }

        public static void ImportFromJson(string path)
        {
            // Clear existing settings
            AssetBundleConfiguration config = GetAssetBundleConfiguration();
            foreach (AssetBundleEntry bundle in config.assetBundles)
            {
                foreach (string asset in bundle.assetPaths)
                {
                    AssetImporter importer = AssetImporter.GetAtPath(asset);
                    importer.SetAssetBundleNameAndVariant("", "");
                }
            }

            // Apply loaded settings
            string json = File.ReadAllText(path);
            config = JsonUtility.FromJson<AssetBundleConfiguration>(json);
            foreach (AssetBundleEntry bundle in config.assetBundles)
            {
                foreach (string asset in bundle.assetPaths)
                {
                    AssetImporter importer = AssetImporter.GetAtPath(asset);
                    importer.SetAssetBundleNameAndVariant(bundle.assetBundleName, bundle.assetBundleVariant);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
