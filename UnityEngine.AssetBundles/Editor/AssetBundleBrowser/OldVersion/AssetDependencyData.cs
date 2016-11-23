using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.AssetBundles
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
			public int[] dependencies;  //indices of dependencies
		}

		public AssetDependencyData()
		{
			Create();
		}

		public void Create()
		{
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
			int assetIndex = FindAssetIndex(asset);
			List<string> results = new List<string>();
            if (assets[assetIndex].dependencies == null)
            {
                    var filtered = AssetDatabase.GetDependencies(assets[assetIndex].name, false).Where(a => a != assets[assetIndex].name);
                    assets[assetIndex].dependencies = new int[filtered.Count()];
                    int di = 0;
                    foreach (var d in filtered)
                        assets[assetIndex].dependencies[di++] = FindAssetIndex(d);
            }
            for (int i = 0; i < assets[assetIndex].dependencies.Length; i++)
				results.Add(assets[assets[assetIndex].dependencies[i]].name);
			return results;
		}
	}
}
