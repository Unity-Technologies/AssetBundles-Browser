using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.AssetBundles.AssetBundleOperation
{
	public class AssetDatabaseABOperation : ABOperation
    {
		public string Name {
			get {
				return "Default";
			}
		}

		public string ProviderName {
			get {
				return "Built-in";
			}
		}

		public string[] GetAssetPathsFromAssetBundle (string assetBundleName) {
			return AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName);
		}

		public string GetAssetBundleName(string assetPath) {
			var importer = AssetImporter.GetAtPath(assetPath);
			if (importer == null) {
				return string.Empty;
			}
			var bundleName = importer.assetBundleName;
			if (importer.assetBundleVariant.Length > 0) {
				bundleName = bundleName + "." + importer.assetBundleVariant;
			}
			return bundleName;
		}

		public string GetImplicitAssetBundleName(string assetPath) {
			return AssetDatabase.GetImplicitAssetBundleName (assetPath);
		}

		public string[] GetAllAssetBundleNames() {
			return AssetDatabase.GetAllAssetBundleNames ();
		}

		public bool IsReadOnly() {
			return false;
		}

		public void SetAssetBundleNameAndVariant (string assetPath, string bundleName, string variantName) {
			AssetImporter.GetAtPath(assetPath).SetAssetBundleNameAndVariant(bundleName, variantName);
		}

		public void RemoveUnusedAssetBundleNames() {
			AssetDatabase.RemoveUnusedAssetBundleNames ();
		}

		public bool CanSpecifyBuildTarget { 
			get { return true; } 
		}
		public bool CanSpecifyBuildOutputDirectory { 
			get { return true; } 
		}

		public bool CanSpecifyBuildOptions { 
			get { return true; } 
		}

		public bool BuildAssetBundles (ABBuildInfo info) {
			BuildPipeline.BuildAssetBundles(info.outputDirectory, info.options, info.buildTarget);

			return true;
		}
    }

	[CustomABOperationProvider("Default", 0)]
	public class AssetDatabaseABOperationProvider : ABOperationProvider
	{
		public int GetABOperationCount () {
			return 1;
		}
		public ABOperation CreateOperation(int index) {
			return new AssetDatabaseABOperation ();
		}
	}
}
