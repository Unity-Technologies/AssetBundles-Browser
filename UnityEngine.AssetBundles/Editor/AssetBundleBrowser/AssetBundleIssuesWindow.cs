using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System;
using System.Linq;

namespace UnityEngine.AssetBundles
{
	internal class AssetBundleIssuesWindow : EditorWindow
	{
		[SerializeField]
		TreeViewState m_treeState;
		IssueTree m_tree;
		

		internal static void ShowWindow()
		{
			var window = GetWindow<AssetBundleIssuesWindow>();
			window.titleContent = new GUIContent("Asset Bundle Issues");
            window.Show();
		}

		class IssueTree : TreeView
		{
			Dictionary<string, List<Issue>> issues;
			public IssueTree(TreeViewState s, Dictionary<string, List<Issue>> i) : base(s)
			{
                issues = i;
			}

			protected override TreeViewItem BuildRoot()
			{
				var root = new TreeViewItem(-1, -1);
                root.children = new List<TreeViewItem>();
				foreach (var c in issues)
				{
					var cat = new TreeViewItem(c.Key.GetHashCode(), 0, root, c.Key);
					root.AddChild(cat);
					foreach (var i in c.Value)
					{
						var item = new TreeViewItem(i.GetHashCode(), 1, cat, i.name);
						cat.AddChild(item);
						foreach (var si in i.subItems)
							item.AddChild(new TreeViewItem(si.GetHashCode(), 2, item, si));
					}
				}
				return root;
			}
		}

		/*
		 * mismatched variant bundles
		 * duplicated assets
         * scenes and assets mixed
         * 
		*/

		public class AssetDependencyData
		{
			public BundleInfo[] bundles;
			public AssetInfo[] assets;

			public struct BundleInfo
			{
				public string name;         //name of bundle
				public int[] assets;        //indices of explicitely included assets
			}

			public struct AssetInfo
			{
				public string name;         //full path name of asset: Assets/foo/bar.png
				public int bundle;          //index of bundle, -1 for none
				public int[] dependencies;  //indices of dependencies
				public HashSet<int> bundles;
			}

			public AssetDependencyData()
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
				{
					assets[i].name = assetPaths[i];
					assets[i].bundle = -1;
				}

				//link assets to bundles
				for (int i = 0; i < bundles.Length; i++)
				{
					string[] assetPathsInBundle = AssetDatabase.GetAssetPathsFromAssetBundle(bundles[i].name);
					bundles[i].assets = new int[assetPathsInBundle.Length];
					for (int a = 0; a < assetPathsInBundle.Length; a++)
						assets[bundles[i].assets[a] = FindAssetIndex(assetPathsInBundle[a])].bundle = i;
				}

				//find asset dependencies
				for (int i = 0; i < assets.Length; i++)
				{
					var filtered = AssetDatabase.GetDependencies(assets[i].name, false).Where(a => a != assets[i].name);
					assets[i].dependencies = new int[filtered.Count()];
					int di = 0;
					foreach (var d in filtered)
						assets[i].dependencies[di++] = FindAssetIndex(d);
				}
			}

			int FindAssetIndex(string a)
			{
				for (int i = 0; i < assets.Length; i++)
					if (assets[i].name == a)
						return i;
				return -1;
			}

			internal void CollectDependencies(int a, HashSet<int> deps)
			{
				var ai = assets[a];
				for (int i = 0; i < ai.dependencies.Length; i++)
				{
					if (deps.Add(ai.dependencies[i]))
					{
						CollectDependencies(i, deps);
					}
				}
			}
		}


		class Issue
		{
			public string name;
			public List<string> subItems = new List<string>();
			public Issue(string n)
			{
				name = n;
			}
		}

		Dictionary<string, List<Issue>> FindIssues()
		{
			List<int> assetsWithDuplicates = new List<int>();
			AssetDependencyData add = new AssetDependencyData();
			for (int bi = 0; bi < add.bundles.Length; bi++)
			{
				for (int ai = 0; ai < add.bundles[bi].assets.Length; ai++)
				{
					var assetIndex = add.bundles[bi].assets[ai];
					HashSet<int> dependencies = new HashSet<int>();
					add.CollectDependencies(assetIndex, dependencies);
					foreach (var d in dependencies)
					{
						if (add.assets[d].bundle < 0)
						{
							if (add.assets[d].bundles == null)
								add.assets[d].bundles = new HashSet<int>();
							add.assets[d].bundles.Add(bi);
							if (add.assets[d].bundles.Count == 2) //once there are two, there is a duplicate, no need to add after that
								assetsWithDuplicates.Add(d);
						}
					}
				}
			}
			var issues = new Dictionary<string, List<Issue>>();
			foreach (var ai in assetsWithDuplicates)
			{
				var issue = new Issue(add.assets[ai].name);
				foreach (var bi in add.assets[ai].bundles)
					issue.subItems.Add(add.bundles[bi].name);

				if (!issues.ContainsKey("Duplicated Assets"))
					issues.Add("Duplicated Assets", new List<Issue>());
				issues["Duplicated Assets"].Add(issue);
			}
			return issues;
		}

		void OnGUI()
		{
            if (m_treeState == null)
				m_treeState = new TreeViewState();
            if(m_tree == null)
				(m_tree = new IssueTree(m_treeState, FindIssues())).Reload();
			GUILayout.BeginVertical();
			m_tree.OnGUI(new Rect(0, 0, position.width, position.height));
			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh"))
                m_tree = null;
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}
	}
}
