using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.UI;
using UnityEditor.IMGUI.Controls;
using System;

/*
 * DONE allow setting asset bundle via multi selection
 * create new bundles
 * delete/rename bundles
 * add warning icon for issues
 * somehow show things pulled in from Resources folders?
 * add refs to unused assets
 * show progress bar when rebuilding
 * handle bundle changes without rebuilding everything
 * 
*/
namespace UnityEngine.AssetBundles
{
	public class AssetBundleData
	{
		public Dictionary<string, AssetInfo> assetInfoMap = new Dictionary<string, AssetInfo>();
		public AssetTreeItemData rootTreeItem;
		static int idCount = 1;
		public int issueCount = 0;
		public AssetBundleData()
		{
			EditorUtility.DisplayProgressBar("Asset Bundle Window", "Refreshing asset database.", 0);
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			DateTime start = DateTime.Now;
			idCount = 1;
			EditorUtility.DisplayProgressBar("Asset Bundle Window", "Gathering asset dependencies", .25f);
			CreateBundleInfos();
			CreateAssetInfos();

			EditorUtility.DisplayProgressBar("Asset Bundle Window", "Creating asset tree data", .75f);
			rootTreeItem = new AssetTreeItemData(this, null, assetInfoMap[string.Empty]);
			foreach (var a in assetInfoMap.Values)
				a.PostProcess(this);
			rootTreeItem.PostProcess(this);

			TimeSpan elapsed = DateTime.Now - start;
			Debug.Log("Rebuilt data for " + assetInfoMap.Count + " entries in " + elapsed.TotalSeconds + " seconds.");
			EditorUtility.ClearProgressBar();
		}

		private void CreateAssetInfos()
		{
			EditorUtility.DisplayProgressBar("Asset Bundle Window", "Gathering asset dependencies", .25f);
			string[] paths = AssetDatabase.GetAllAssetPaths();
			for(int i = 0; i < paths.Length; i++)
			{
				if(i % 100 == 0)
					EditorUtility.DisplayProgressBar("Asset Bundle Window", "Gathering asset dependencies", .25f + ((float)i / (float)paths.Length) * .5f);
				string asset = paths[i];
				string ext = System.IO.Path.GetExtension(asset);
				if (ext.Length > 0 && ext != ".dll" && ext != ".cs" && !asset.StartsWith("ProjectSettings") && !asset.StartsWith("Library"))
					assetInfoMap.Add(asset, new AssetInfo(this, asset, AssetInfo.Type.Asset, AssetDatabase.GetDependencies(asset), false));
			}
		}

		private void CreateBundleInfos()
		{
			string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
			assetInfoMap.Add(string.Empty, new AssetInfo(this, string.Empty, AssetInfo.Type.BundlePath, new string[] { }, false));

			foreach (var bundleName in bundleNames)
			{
				List<string> parts = new List<string>(bundleName.Split('/'));
				bool isVariant = false;
				int vi = parts.Last().IndexOf('.');
				if (vi > 0)
				{
					isVariant = true;
					string last = parts.Last();
					parts.RemoveAt(parts.Count - 1);
					parts.Add(last.Substring(0, vi));
					parts.Add(last.Substring(vi + 1));
				}
				AssetInfo parent = assetInfoMap[string.Empty];
				for (int p = 0; p < parts.Count; p++)
				{
					string n = parts[p];
					for (int j = p-1; j >= 0; j--)
						n = parts[j] + ((isVariant && p == parts.Count - 1 && j == p - 1) ? "." : "/") + n;
					if (!parent.dependencies.Contains(n))
						parent.dependencies.Add(n);
					if (!assetInfoMap.TryGetValue(n, out parent))
					{
						if (n == bundleName)
						{
							assetInfoMap.Add(n, new AssetInfo(this, n, AssetInfo.Type.Bundle, AssetDatabase.GetAssetPathsFromAssetBundle(n), isVariant));
						}
						else
						{
							if (!assetInfoMap.ContainsKey(n))
							{
								assetInfoMap.Add(n, parent = new AssetInfo(this, n, AssetInfo.Type.BundlePath, new string[] { }, false));
							}
						}
					}
				}
			}
		}

		public void Refresh()
		{
			DateTime start = DateTime.Now;
			idCount = 1;
			foreach (var a in assetInfoMap.Values)
				a.Reset();
			foreach (var a in assetInfoMap.Values)
				a.PostProcess(this);
			rootTreeItem = new AssetTreeItemData(this, null, assetInfoMap[string.Empty]);

			rootTreeItem.PostProcess(this);

			TimeSpan elapsed = DateTime.Now - start;
			Debug.Log("Refreshed data for " + assetInfoMap.Count + " entries in " + elapsed.TotalSeconds + " seconds.");
		}

		public class AssetTreeItemData
		{
			public int id;
			public List<string> issues = new List<string>();
			public int childIssueCount = 0;
			public AssetInfo assetInfo;
			public AssetTreeItemData parent;
			public List<AssetTreeItemData> children = new List<AssetTreeItemData>();
			public AssetTreeItemData(AssetBundleData abd, AssetTreeItemData p, AssetInfo i)
			{
				parent = p;
				id = idCount++;
				assetInfo = i;
				foreach (var d in assetInfo.dependencies)
				{
					children.Add(new AssetTreeItemData(abd, this, abd.assetInfoMap[d]));
					abd.assetInfoMap[d].references.Add(CreateReferenceList());
				}
			}

			public List<string> CreateReferenceList()
			{
				List<string> refs = new List<string>();
				refs.Add(assetInfo.assetName);
				AssetTreeItemData p = parent;
				while (p != null && p.assetInfo.assetName.Length > 0 && (p.assetInfo.type == AssetInfo.Type.Bundle || p.assetInfo.type == AssetInfo.Type.Asset))
				{
					refs.Insert(0, p.assetInfo.assetName);
					p = p.parent;
				}
				return refs;
			}

			public int CountIssues()
			{
				if (IsDuplicated)
				{
					string errMsg = "Asset duplicated in multiple bundles:\n\t";
					foreach (var r in assetInfo.uniqueRoots)
						errMsg += r + "\n\t";
				
					issues.Add(errMsg);
				}
				childIssueCount = 0;
				if (children != null)
				{
					foreach (var c in children)
						childIssueCount += c.CountIssues();
				}
				return childIssueCount + issues.Count;
			}

			internal void PostProcess(AssetBundleData abd)
			{
				foreach (var c in children)
					c.FindRootDependencies();
				CountIssues();
			}

			private void FindRootDependencies()
			{
				if (assetInfo.type == AssetInfo.Type.Bundle)
				{
					assetInfo.FindRootDependencies();
					if (assetInfo.isVariant)
					{
						foreach (var c in parent.children)
						{
							if (c != this && !CompareLists(c.assetInfo.dependencies, assetInfo.dependencies))
								issues.Add("Variant bundle contents do not match variant " + c.assetInfo.assetName);
						}
					}
				}
				if (assetInfo.type == AssetInfo.Type.BundlePath)
				{
					foreach (var c in children)
						c.FindRootDependencies();
				}
			}

			private bool CompareLists(List<string> a, List<string> b)
			{
				if (a.Count != b.Count)
					return false;
				for (int i = 0; i < a.Count; i++)
				{
					string ap = System.IO.Path.GetFileNameWithoutExtension(a[i]);
					bool found = false;
					for (int j = 0; j < b.Count; j++)
					{
						string bp = System.IO.Path.GetFileNameWithoutExtension(b[i]);
						if (ap == bp)
						{
							found = true;
							break;
						}
					}
					if (!found)
						return false;
				}
				return true;
			}

			public string displayName
			{
				get
				{
					int issueCount = childIssueCount + issues.Count;
					if (issueCount == 0)
						return assetInfo.displayName;
					return assetInfo.displayName + "<color=red> [" + issueCount + " issue" + (issueCount > 1 ? "s" : "") + "]" + "</color>"; ;
				}
			}

			public bool IsDuplicated
			{
				get
				{
					return string.IsNullOrEmpty(assetInfo.rootReference) && assetInfo.uniqueRoots.Count > 1;
				}
			}
		}

		public class AssetInfo
		{
			AssetBundleData abData;
			public bool isVariant;
			public string assetName;
			public enum Type
			{
				None,
				BundlePath,
				Bundle,
				Asset
			}

			public Type type;
			public string rootReference = string.Empty;
			public List<string> uniqueRoots = new List<string>();
			public List<List<string>> references = new List<List<string>>(); //first item is parent, last is root
			public List<string> dependencies = new List<string>();
			public List<string> rootDependencies = new List<string>();
			public AssetInfo(AssetBundleData abd, string name, Type t, IEnumerable<string> deps, bool isVar)
			{
				isVariant = isVar;
				abData = abd;
				type = t;
				assetName = name;
				foreach (var d in deps)
					if (d != assetName)
						dependencies.Add(d);
			}

			internal void Reset()
			{
				references.Clear();
				uniqueRoots.Clear();
				rootDependencies.Clear();
				rootReference = "";
			}
			public void PostProcess(AssetBundleData abd)
			{
				uniqueRoots.Clear();
				rootDependencies.Clear();
				rootReference = "";
				if (type == Type.Bundle)
					return;

				foreach (var r in references)
				{
					if (!abData.assetInfoMap.ContainsKey(r.Last()))
						Debug.Log("Cannot find asset " + r.Last());
					AssetInfo p = abData.assetInfoMap[r.Last()];
					if (p.type == Type.Bundle && r.Count == 1)
						rootReference = r.Last();
					if (!uniqueRoots.Contains(r.First()))
						uniqueRoots.Add(r.First());
				}
			}

			public string GetFullReferencePath(List<string> refs)
			{
				string p = "<color=white>";
				string sep = "";
				foreach (var s in refs)
				{
					AssetInfo ai = abData.assetInfoMap[s];
					p += sep + (ai.type == AssetInfo.Type.Asset ? ai.displayName : ai.displayPath);
					sep = "/";
				}
				return p + "</color>";
			}

			public string displayPath
			{
				get
				{
					if (assetName.StartsWith("Assets/"))
						return ColorName(assetName.Substring("Assets/".Length));
					return ColorName(assetName);
				}
			}

			public string displayName
			{
				get
				{
					if (type == Type.Bundle || type == Type.BundlePath)
					{
						int i = assetName.LastIndexOf('.');
						if(i > 0)
							return ColorName(assetName.Substring(i+1));
						return ColorName(System.IO.Path.GetFileName(assetName));
					}
					return ColorName(System.IO.Path.GetFileNameWithoutExtension(assetName));
				}
			}

			private string ColorName(string n)
			{
				if (type == Type.Bundle || type == Type.BundlePath)
				{
					return "<b><color=white>" + n + "</color></b>";
				}
				string color = (!string.IsNullOrEmpty(rootReference)) ? "white" : (uniqueRoots.Count > 1 ? "red" : "grey");
				return "<color=" + color + ">" + n + "</color>";
			}

			internal void FindRootDependencies()
			{
				foreach (var d in dependencies)
				{
					AssetInfo i = abData.assetInfoMap[d];
					i.AddRoots(rootDependencies, assetName);
				}
			}

			private void AddRoots(List<string> rd, string exclude)
			{
				if (!string.IsNullOrEmpty(rootReference) && rootReference != exclude && !rd.Contains(rootReference))
					rd.Add(rootReference);

				foreach (var r in dependencies)
					abData.assetInfoMap[r].AddRoots(rd, exclude);
			}

		}
	}
}