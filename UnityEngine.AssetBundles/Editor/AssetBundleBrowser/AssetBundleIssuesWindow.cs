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
        const string kMismatchedVariant = "Mismatched Variant Bundles";
        const string kMixedScene = "Mixed Scene Bundles";
        const string kDuplicateAssets = "Duplicated Assets";

        [MenuItem("AssetBundles/Analyze", priority = 1)]
        internal static void ShowWindow()
		{
			var window = GetWindow<AssetBundleIssuesWindow>();
			window.titleContent = new GUIContent("ABAnalyze");
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

            protected override void RowGUI(RowGUIArgs args)
            {
                Color oldColor = GUI.color;
                if (args.item != null && args.item.depth == 0)
                    GUI.color = (args.item.children != null && args.item.children.Count > 0) ? Color.red : Color.green;
                base.RowGUI(args);
                GUI.color = oldColor;

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
            var issues = new Dictionary<string, List<Issue>>();
            issues.Add(kDuplicateAssets, new List<Issue>());
            issues.Add(kMismatchedVariant, new List<Issue>());
            issues.Add(kMixedScene, new List<Issue>());

            for (int bi = 0; bi < add.bundles.Length; bi++)
			{
                string sceneAssetName = null;
                bool hasScene = false;
                bool hasNonScene = false;
				for (int ai = 0; ai < add.bundles[bi].assets.Length; ai++)
				{
					var assetIndex = add.bundles[bi].assets[ai];
                    if (!hasScene || !hasNonScene)
                    {
                        if (AssetDatabase.GetMainAssetTypeAtPath(add.assets[assetIndex].name) == typeof(SceneAsset))
                        {
                            hasScene = true;
                            sceneAssetName = add.assets[assetIndex].name;
                        }
                        else
                            hasNonScene = true;
                    }

                    HashSet < int> dependencies = new HashSet<int>();
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
                if (hasNonScene && hasScene)
                {
                    var issue = new Issue(add.bundles[bi].name);
                    issue.subItems.Add(sceneAssetName);
                    issues[kMixedScene].Add(issue);
                }
            }

            for (int bi = 0; bi < add.bundles.Length; bi++)
            {
                int dot = add.bundles[bi].name.LastIndexOf('.');
                if (dot > 0)
                {
                    var baseName = add.bundles[bi].name.Substring(0, dot);
                    for (int bi2 = bi + 1; bi2 < add.bundles.Length; bi2++)
                    {
                        if (bi2 == bi)
                            continue;
                        int dot2 = add.bundles[bi2].name.LastIndexOf('.');
                        if (dot2 <= 0)
                            continue;
                        if (!CompareVariantAssetLists(add, add.bundles[bi].assets, add.bundles[bi2].assets))
                        {
                            Issue issue = null;
                            foreach (var i in issues[kMismatchedVariant])
                            {
                                if (i.name == baseName)
                                {
                                    issue = i;
                                    break;
                                }
                            }

                            if (issue == null)
                            {
                                issue = new Issue(baseName);
                                issues[kMismatchedVariant].Add(issue);
                            }

                            if (!issue.subItems.Contains(add.bundles[bi].name))
                                issue.subItems.Add(add.bundles[bi].name);
                            if (!issue.subItems.Contains(add.bundles[bi2].name))
                                issue.subItems.Add(add.bundles[bi2].name);
                        }
                    }

                    for (int ai = 0; ai < add.bundles[bi].assets.Length; ai++)
                    {
                        var assetIndex = add.bundles[bi].assets[ai];
                    }
                }
            }

            foreach (var ai in assetsWithDuplicates)
			{
				var issue = new Issue(add.assets[ai].name);
				foreach (var bi in add.assets[ai].bundles)
					issue.subItems.Add(add.bundles[bi].name);

				issues[kDuplicateAssets].Add(issue);
			}
			return issues;
		}

        private bool CompareVariantAssetLists(AssetDependencyData add, int[] a1, int[] a2)
        {
            if ((a1 == null) != (a2 == null))
                return false;
            if (a1.Length != a2.Length)
                return false;
            for (int ai = 0; ai < a1.Length; ai++)
            {
                var lower = System.IO.Path.GetFileNameWithoutExtension(add.assets[a1[ai]].name).ToLower();
                bool match = false;
                for (int ai2 = 0; ai2 < a2.Length; ai2++)
                {
                    if (lower == System.IO.Path.GetFileNameWithoutExtension(add.assets[a2[ai2]].name).ToLower())
                    {
                        match = true;
                        break;
                    }
                }
                if (!match)
                    return false;
            }
            return true;
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
