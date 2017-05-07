using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.AssetBundles.AssetBundleOperation
{
	public struct ABBuildInfo {
		public string outputDirectory;
		public BuildAssetBundleOptions options;
		public BuildTarget buildTarget;
	}

	public interface ABOperation
    {
		string Name { get; }
		string ProviderName { get; }

		string[] GetAssetPathsFromAssetBundle (string assetBundleName);
		string GetAssetBundleName(string assetPath);
		string GetImplicitAssetBundleName(string assetPath);
		string[] GetAllAssetBundleNames();
		bool IsReadOnly();

		void SetAssetBundleNameAndVariant (string assetPath, string bundleName, string variantName);
		void RemoveUnusedAssetBundleNames();

		bool CanSpecifyBuildTarget { get; }
		bool CanSpecifyBuildOutputDirectory { get; }
		bool CanSpecifyBuildOptions { get; }

		bool BuildAssetBundles (ABBuildInfo info);
    }
}
