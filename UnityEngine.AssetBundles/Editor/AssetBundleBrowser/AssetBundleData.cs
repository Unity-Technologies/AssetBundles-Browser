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
namespace UnityEditor.AssetBundles
{
	public class AssetBundleData
	{
		public Dictionary<string, AssetInfo> assetInfoMap = new Dictionary<string, AssetInfo>();
		public List<AssetTreeItemData> treeItemData = new List<AssetTreeItemData>();
		static int idCount = 1;
		public int issueCount = 0;
		public AssetBundleData()
		{
			DateTime start = DateTime.Now;
			idCount = 1;
			treeItemData = new List<AssetTreeItemData>();
			foreach (var bundleName in AssetDatabase.GetAllAssetBundleNames())
				assetInfoMap.Add(bundleName, new AssetInfo(this, bundleName, AssetInfo.Type.Bundle));

			foreach (var bundleName in AssetDatabase.GetAllAssetBundleNames())
				treeItemData.Add(new AssetTreeItemData(this, null, assetInfoMap[bundleName]));

			foreach (var k in assetInfoMap)
				k.Value.PostProcess();

			foreach (var t in treeItemData)
				issueCount += t.CountIssues();

			foreach (var t in treeItemData)
				t.assetInfo.FindRootDependencies();

			AssetInfo unreferenced = new AssetInfo(this, "Unreferenced", AssetInfo.Type.None);
			assetInfoMap.Add(unreferenced.assetName, unreferenced);
			treeItemData.Add(new AssetTreeItemData(this, null, unreferenced));
			TimeSpan elapsed = DateTime.Now - start;
			Debug.Log("Rebuilt data for " + assetInfoMap.Count + " entries in " + elapsed.TotalSeconds + " seconds.");
		}
		public void Refresh()
		{
			DateTime start = DateTime.Now;
			idCount = 1;
			treeItemData = new List<AssetTreeItemData>();
			foreach (var bundleName in AssetDatabase.GetAllAssetBundleNames())
				assetInfoMap[bundleName] = new AssetInfo(this, bundleName, AssetInfo.Type.Bundle);

			foreach (var bundleName in AssetDatabase.GetAllAssetBundleNames())
				treeItemData.Add(new AssetTreeItemData(this, null, assetInfoMap[bundleName]));

			foreach (var k in assetInfoMap)
				k.Value.PostProcess();

			foreach (var t in treeItemData)
				issueCount += t.CountIssues();

			foreach (var t in treeItemData)
				t.assetInfo.rootDependencies.Clear();

			foreach (var t in treeItemData)
				t.assetInfo.FindRootDependencies();

			AssetInfo unreferenced = new AssetInfo(this, "Unreferenced", AssetInfo.Type.None);
			assetInfoMap[unreferenced.assetName] = unreferenced;
			treeItemData.Add(new AssetTreeItemData(this, null, unreferenced));
			TimeSpan elapsed = DateTime.Now - start;
			Debug.Log("Refreshed data for " + assetInfoMap.Count + " entries in " + elapsed.TotalSeconds + " seconds.");
		}

		//there are one of these per TreeViewItem
		public class AssetTreeItemData
		{
			public int id;
			public int issueCount = 0;
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
				while (p != null)
				{
					refs.Insert(0, p.assetInfo.assetName);
					p = p.parent;
				}
				return refs;
			}

			public int CountIssues()
			{
				issueCount = IsDuplicated ? 1 : 0;
				childIssueCount = 0;
				foreach (var c in children)
					childIssueCount += c.CountIssues();
				return childIssueCount + issueCount;
			}

			public string displayName
			{
				get
				{
					if (childIssueCount == 0)
						return assetInfo.displayName;
					return assetInfo.displayName + "<color=red> [" + childIssueCount + " issue" + (childIssueCount > 1 ? "s" : "") + "]" + "</color>"; ;
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
			public string assetName;
			public enum Type
			{
				None,
				Bundle,
				Asset
			}

			public Type type;
			public string rootReference = string.Empty;
			public List<string> uniqueRoots = new List<string>();
			public List<List<string>> references = new List<List<string>>(); //first item is parent, last is root
			public List<string> dependencies = new List<string>();
			public List<string> rootDependencies = new List<string>();
			public AssetInfo(AssetBundleData abd, string name, Type t)
			{
				abData = abd;
				type = t;
				assetName = name;
				if (type == Type.Asset)
				{
					foreach (var d in AssetDatabase.GetDependencies(assetName, false))
					{
						dependencies.Add(d);
						if (!abData.assetInfoMap.ContainsKey(d))
							abData.assetInfoMap.Add(d, new AssetInfo(abData, d, Type.Asset));
					}
				}
				else if (type == Type.None)
				{
					List<string> unref = new List<string>();
					foreach (var d in AssetDatabase.GetAllAssetPaths())
					{
						if (!d.StartsWith("Assets/"))
							continue;
						string ext = System.IO.Path.GetExtension(d);
						if (ext.Length == 0 || ext == ".dll" || ext == ".cs")
							continue;

						if (!abData.assetInfoMap.ContainsKey(d))
						{
							abData.assetInfoMap.Add(d, new AssetInfo(abData, d, Type.Asset));
							dependencies.Add(d);
						}
					}
				}
				else if (type == Type.Bundle)
				{
					foreach (var d in AssetDatabase.GetAssetPathsFromAssetBundle(assetName))
					{
						dependencies.Add(d);
						if (!abData.assetInfoMap.ContainsKey(d))
							abData.assetInfoMap.Add(d, new AssetInfo(abData, d, Type.Asset));
					}
				}
			}

			public void PostProcess()
			{
				if (type == Type.Bundle)
				{
					rootReference = "";
					return;
				}

				uniqueRoots.Clear();
				foreach (var r in references)
				{
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
					p += sep + abData.assetInfoMap[s].displayName;
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
					if (type == Type.Bundle)
						return ColorName(assetName);
					return ColorName(System.IO.Path.GetFileNameWithoutExtension(assetName));
				}
			}

			private string ColorName(string n)
			{
				string color = (!string.IsNullOrEmpty(rootReference) || type == Type.Bundle) ? "white" : (uniqueRoots.Count > 1 ? "red" : "grey");
				if (type == Type.Bundle)
					return "<b><color=" + color + ">" + n + "</color></b>";
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