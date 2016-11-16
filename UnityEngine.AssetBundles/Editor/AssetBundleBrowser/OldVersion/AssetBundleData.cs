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
 * DONE show progress bar when rebuilding
 * handle bundle changes without rebuilding everything
 * remove asset from bundle
*/
namespace UnityEngine.AssetBundles.Old
{
	public class AssetBundleData
	{
		AssetDependencyData dependencyData;
		public Dictionary<string, AssetInfo> assetInfoMap = new Dictionary<string, AssetInfo>();
		public AssetTreeItemData rootTreeItem;
		static int idCount = 1;
		public int issueCount = 0;
		public bool isValid = false;
		public AssetBundleData()
		{
		}

		public void Refresh(bool rebuildDependencies)
		{
			if (rebuildDependencies)
				dependencyData = null;
			if (dependencyData == null)
				dependencyData = new AssetDependencyData();
			assetInfoMap = new Dictionary<string, AssetInfo>();
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			idCount = 1;
			try
			{
				assetInfoMap.Add(string.Empty, new AssetInfo(this, string.Empty, AssetInfo.Type.BundlePath, new string[] { }, false));

				CreateBundleInfos();
				CreateAssetInfos();

				rootTreeItem = new AssetTreeItemData(this, null, assetInfoMap[string.Empty], 0);
				foreach (var a in assetInfoMap.Values)
					a.PostProcess(this);
				rootTreeItem.PostProcess(this);
				
				AssetTreeItemData unrefs = new AssetTreeItemData(this, rootTreeItem, new AssetInfo(this, "Unreferenced", AssetInfo.Type.None, new string[] { }, false), 0);
				rootTreeItem.children.Add(unrefs);

				Dictionary<string, AssetTreeItemData> assetTypes = new Dictionary<string, AssetTreeItemData>();

				foreach (var a in assetInfoMap.Values)
				{
					if (a.references.Count == 0 && a.type == AssetInfo.Type.Asset)
					{
						string typeName = AssetDatabase.GetMainAssetTypeAtPath(a.assetName).Name;
						//var importer = AssetImporter.GetAtPath(a.assetName);
						AssetTreeItemData p;
						if (!assetTypes.TryGetValue(typeName, out p))
						{
							p = new AssetTreeItemData(this, unrefs, new AssetInfo(this, typeName, AssetInfo.Type.None, new string[] { }, false), 0);
							unrefs.children.Add(p);
							assetTypes.Add(typeName, p);
						}

						p.children.Add(new AssetTreeItemData(this, p, a, 0));
					}
				}
				
				isValid = true;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		private void CreateAssetInfos()
		{
			string[] paths = AssetDatabase.GetAllAssetPaths();
			for(int i = 0; i < paths.Length; i++)
			{
				string asset = paths[i];
				string ext = System.IO.Path.GetExtension(asset);
				if (ext.Length > 0 && ext != ".dll" && ext != ".cs" && !asset.StartsWith("ProjectSettings") && !asset.StartsWith("Library"))
					assetInfoMap.Add(asset, new AssetInfo(this, asset, AssetInfo.Type.Asset, dependencyData.GetDependencies(asset), false));
			}
		}

		private void CreateBundleInfos()
		{
			string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();

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

		public class AssetTreeItemData
		{
			public int id;
			public List<string> issues = new List<string>();
			public int childIssueCount = 0;
			public AssetInfo assetInfo;
			public AssetTreeItemData parent;
			public List<AssetTreeItemData> children = new List<AssetTreeItemData>();
			AssetBundleData assetBundleData;
			public AssetTreeItemData(AssetBundleData abd, AssetTreeItemData p, AssetInfo i, int depth)
			{
				parent = p;
				assetBundleData = abd;
				id = idCount++;
				assetInfo = i;
				if (depth > 7)
					return;
				foreach (var d in assetInfo.dependencies)
				{
					if (abd.assetInfoMap.ContainsKey(d))
					{
						//Debug.Log("Unable to find asset " + d);
						children.Add(new AssetTreeItemData(abd, this, abd.assetInfoMap[d], depth + 1));
						abd.assetInfoMap[d].references.Add(CreateReferenceList());
					}
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
					if (!string.IsNullOrEmpty(assetInfo.rootReference))
						return false;
					foreach (var u in assetInfo.uniqueRoots)
					{
						if (!string.IsNullOrEmpty(assetBundleData.assetInfoMap[u].rootReference))
							return false;
					}
					return true;
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
			public long size = 0;
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
			/*	if (System.IO.Path.GetExtension(assetName).Length > 0)
				{
					string path = AssetDatabase.AssetPathToGUID(assetName);
					string libPath = "Library/metadata/" + path.Substring(0, 2) + "/" + path;
					if (System.IO.File.Exists(libPath))
					{
						var s = System.IO.File.OpenRead(libPath);
						size = s.Length;
						s.Close();
					}
				}
				*/foreach (var d in deps)
					dependencies.Add(d);
			}

			internal void Reset()
			{
				references.Clear();
				uniqueRoots.Clear();
				rootDependencies.Clear();
				rootReference = "";
			}

			public long totalSize
			{
				get
				{
					long total = size;
					foreach (var d in dependencies)
						total += abData.assetInfoMap[d].totalSize;
					return total;
				}
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
					i.AddRoots(rootDependencies, assetName, 0);
				}
			}

			private void AddRoots(List<string> rd, string exclude, int depth)
			{
				if (!string.IsNullOrEmpty(rootReference) && rootReference != exclude && !rd.Contains(rootReference))
					rd.Add(rootReference);
				if (depth > 7)
					return;
				foreach (var r in dependencies)
				{
					if (abData.assetInfoMap.ContainsKey(r))
						abData.assetInfoMap[r].AddRoots(rd, exclude, depth + 1);
				}
			}

		}
	}
}