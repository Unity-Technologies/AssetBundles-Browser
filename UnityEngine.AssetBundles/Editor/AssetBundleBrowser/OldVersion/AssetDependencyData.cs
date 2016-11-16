using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.AssetBundles.Old
{

	public class AssetDependencyData
	{
		public BundleInfo[] bundles;
		public AssetInfo[] assets;

		public struct BundleInfo
		{
			public string name;			//name of bundle
			public int[] assets;		//indices of directly included assets
		}

		public struct AssetInfo
		{
			public string name;			//full path name of asset: Assets/foo/bar.png
			public int bundle;          //index of bundle, -1 for none
			//public long size;			//process asset size for current platform
			public int[] dependencies;  //indices of dependencies
		}

		public AssetDependencyData()
		{
			Create();
		}

		public void Create()
		{
			DateTime start = DateTime.Now;
			//find all bundles
			string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
			bundles = new BundleInfo[bundleNames.Length];
			for (int i = 0; i < bundleNames.Length; i++)
				bundles[i].name = bundleNames[i];

			//find all assets
			string[] assetPaths = AssetDatabase.GetAllAssetPaths();
			assets = new AssetInfo[assetPaths.Length];
			for (int i = 0; i < assetPaths.Length; i++)
				assets[i].name = assetPaths[i];

			//link assets to bundles
			for (int i = 0; i < bundles.Length; i++)
			{
				string[] assetPathsInBundle = AssetDatabase.GetAssetPathsFromAssetBundle(bundles[i].name);
				bundles[i].assets = new int[assetPathsInBundle.Length];
				for (int a = 0; a < assetPathsInBundle.Length; a++)
					assets[bundles[i].assets[a] = FindAssetIndex(assetPathsInBundle[a])].bundle = i;
			}

			//link asset dependencies
			for (int i = 0; i < assets.Length; i++)
			{
				if (i % 100 == 0)
					EditorUtility.DisplayProgressBar("Asset Bundle Window", "Gathering asset dependencies", ((float)i / (float)assets.Length));
				
				var filtered = AssetDatabase.GetDependencies(assets[i].name, false).Where(a => a != assets[i].name);
				assets[i].dependencies = new int[filtered.Count()];
				int di = 0;
				foreach(var d in filtered)
					assets[i].dependencies[di++] = FindAssetIndex(d);
			}
			TimeSpan elapsed = DateTime.Now - start;
			EditorUtility.ClearProgressBar();
		}

		int FindAssetIndex(string a)
		{
			for (int i = 0; i < assets.Length; i++)
				if (assets[i].name == a)
					return i;
			return -1;
		}

		internal IEnumerable<string> GetDependencies(string asset)
		{
			int a = FindAssetIndex(asset);
			List<string> results = new List<string>();
			for (int i = 0; i < assets[a].dependencies.Length; i++)
				results.Add(assets[assets[a].dependencies[i]].name);
			return results;
		}
	}
}
