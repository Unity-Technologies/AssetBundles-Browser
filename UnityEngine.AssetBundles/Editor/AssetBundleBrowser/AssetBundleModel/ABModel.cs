using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.AssetBundles.AssetBundleModel
{
    public class Model
    {
        const string kNewBundleBaseName = "newbundle";

        private static BundleFolderInfo m_rootLevelBundles = new BundleFolderInfo("", null);
        private static List<ABMoveData> m_moveData = new List<ABMoveData>();
        private static List<BundleInfo> m_bundlesToUpdate = new List<BundleInfo>();
        private static Dictionary<string, AssetInfo> m_globalAssetList = new Dictionary<string, AssetInfo>();
        private static Dictionary<string, HashSet<string>> m_dependencyTracker = new Dictionary<string, HashSet<string>>();

        public static bool Update()
        {
            bool shouldRepaint = false;
            ExecuteAssetMove();     //this should never do anything. just a safety check.

            //TODO - look into EditorApplication callback functions.
            
            int size = m_bundlesToUpdate.Count;
            if (size > 0)
            {
                m_bundlesToUpdate[size - 1].Update();
                m_bundlesToUpdate.RemoveAll(item => item.DoneUpdating == true);
                if (m_bundlesToUpdate.Count == 0)
                {
                    shouldRepaint = true;
                    foreach(var bundle in m_rootLevelBundles.GetChildList())
                    {
                        bundle.RefreshWarning();
                    }
                }
            }
            return shouldRepaint;
        }
        public static void ForceReloadData(TreeView tree)
        {
            Rebuild();
            tree.Reload();
            bool doneUpdating = m_bundlesToUpdate.Count == 0;
            while (!doneUpdating)
                doneUpdating = Update();
        }
        public static void Rebuild()
        {
            m_rootLevelBundles = new BundleFolderInfo("", null);
            m_moveData = new List<ABMoveData>();
            m_bundlesToUpdate = new List<BundleInfo>();
            m_globalAssetList = new Dictionary<string, AssetInfo>();
            Refresh();
        }
        public static void AddBundlesToUpdate(IEnumerable<BundleInfo> bundles)
        {
            foreach(var bundle in bundles)
            {
                bundle.ForceNeedUpdate();
                m_bundlesToUpdate.Add(bundle);
            }
        }
        public static void Refresh()
        {
            foreach (var bundleName in AssetDatabase.GetAllAssetBundleNames())
            {
                AddBundleToModel(bundleName);
            }
            //m_bundlesToUpdate.AddRange(m_rootLevelBundles.GetChildList());
            AddBundlesToUpdate(m_rootLevelBundles.GetChildList());
        }
        public static bool BundleListIsEmpty()
        {
            return (m_rootLevelBundles.GetChildList().Count() == 0);
        }

        public static BundleInfo CreateEmptyBundle(BundleFolderInfo folder = null, string newName = null)
        {
            folder = (folder == null) ? m_rootLevelBundles : folder;
            string name = newName == null ? GetUniqueName(folder) : newName;
            BundleNameData nameData = new BundleNameData(folder.m_name.Name, name);
            return AddBundleToFolder(folder, nameData);
        }
        public static BundleFolderInfo CreateEmptyBundleFolder(BundleFolderInfo folder = null)
        {
            folder = (folder == null) ? m_rootLevelBundles : folder;
            string name = GetUniqueName(folder) + "/dummy";
            BundleNameData nameData = new BundleNameData(folder.m_name.Name, name);
            return AddFoldersToBundle(m_rootLevelBundles, nameData);
        }

        private static BundleInfo AddBundleToModel(string name)
        {
            if (name == null)
                return null;

            BundleNameData nameData = new BundleNameData(name);

            BundleFolderInfo folder = AddFoldersToBundle(m_rootLevelBundles, nameData);
            BundleInfo currInfo = AddBundleToFolder(folder, nameData);

            return currInfo;
        }
        private static BundleFolderInfo AddFoldersToBundle(BundleFolderInfo root, BundleNameData nameData)
        {
            BundleInfo currInfo = root;
            var size = nameData.NameTokens.Count;
            for (var index = 0; index < size - 1; index++)
            {
                var folder = currInfo as BundleFolderInfo;
                if (folder != null)
                {
                    currInfo = folder.GetChild(nameData.NameTokens[index]);
                    if (currInfo == null)
                    {
                        currInfo = new BundleFolderInfo(nameData.NameTokens, index + 1, folder);
                        folder.AddChild(currInfo);
                    }
                }
            }
            return currInfo as BundleFolderInfo;
        }
        private static BundleInfo AddBundleToFolder(BundleFolderInfo root, BundleNameData nameData)
        {
            BundleInfo currInfo = root.GetChild(nameData.ShortName);
            if (currInfo == null)
            {
                currInfo = new BundleDataInfo(nameData.Name, root);
                root.AddChild(currInfo);
            }
            return currInfo;
        }

        private static string GetUniqueName(BundleFolderInfo folder)
        {
            string name = kNewBundleBaseName;
            int index = 1;
            bool foundExisting = (folder.GetChild(name) != null);
            while (foundExisting)
            {
                name = kNewBundleBaseName + index;
                index++;
                foundExisting = (folder.GetChild(name) != null);
            }
            return name;
        }

        public static BundleTreeItem CreateBundleTreeView()
        {
            return m_rootLevelBundles.CreateTreeView(-1);
        }
        public static AssetTreeItem CreateAssetListTreeView(IEnumerable<AssetBundleModel.BundleInfo> selectedBundles)
        {
            //m_bundlesToUpdate.AddRange(selectedBundles);
            var root = new AssetTreeItem();
            if (selectedBundles != null)
            {
                foreach (var bundle in selectedBundles)
                {
                    bundle.AddAssetsToNode(root);
                }
            }
            return root;
        }

        public static bool HandleBundleRename(BundleTreeItem item, string newName)
        {
            item.bundle.HandleRename(newName, 0);
            ExecuteAssetMove();
            return true;  //is there an illegal rename?  if so, return false.
        }

        public static void HandleBundleReparent(IEnumerable<BundleInfo> bundles, BundleFolderInfo parent)
        {
            foreach (var bundle in bundles)
            {
                bundle.HandleReparent(parent.m_name.Name);
            }
            ExecuteAssetMove();
            Rebuild();
        }

        public static void HandleBundleMerge(IEnumerable<BundleInfo> bundles, BundleDataInfo target)
        {
            foreach (var bundle in bundles)
            {
                bundle.HandleDelete(true, target.m_name.Name);
            }
            ExecuteAssetMove();
        }

        public static void HandleBundleDelete(IEnumerable<BundleInfo> bundles)
        {
            foreach (var bundle in bundles)
            {
                bundle.HandleDelete(true);
            }
            ExecuteAssetMove();
        }

        public static BundleInfo HandleDedupeBundles(IEnumerable<BundleInfo> bundles)
        {
            var newBundle = CreateEmptyBundle();
            HashSet<string> dupeAssets = new HashSet<string>();
            HashSet<string> fullAssetList = new HashSet<string>();

            //if they were just selected, then they may still be updating.
            bool doneUpdating = m_bundlesToUpdate.Count == 0;
            while (!doneUpdating)
                doneUpdating = Update();

            foreach (var bundle in bundles)
            {
                foreach (var asset in bundle.GetDependencies())
                {
                    if (!fullAssetList.Add(asset.Name))
                        dupeAssets.Add(asset.Name);
                }
            }


            MoveAssetToBundle(dupeAssets, newBundle.m_name.Name);
            ExecuteAssetMove();
            return newBundle;
        }

        class ABMoveData
        {
            public string m_asset;
            public string m_bundle;
            public ABMoveData(string asset, string bundle)
            {
                m_asset = asset;
                m_bundle = bundle;
            }
            public void Apply()
            {
                //TODO support variants
                AssetImporter.GetAtPath(m_asset).SetAssetBundleNameAndVariant(m_bundle, string.Empty);
            }
        }
        public static void MoveAssetToBundle(AssetInfo asset, string bundleName)
        {
            m_moveData.Add(new ABMoveData(asset.Name, bundleName));
        }
        public static void MoveAssetToBundle(string assetName, string bundleName)
        {
            m_moveData.Add(new ABMoveData(assetName, bundleName));
        }
        public static void MoveAssetToBundle(IEnumerable<AssetInfo> assets, string bundleName)
        {
            foreach (var asset in assets)
                MoveAssetToBundle(asset, bundleName);
        }
        public static void MoveAssetToBundle(IEnumerable<string> assetNames, string bundleName)
        {
            foreach (var assetName in assetNames)
                MoveAssetToBundle(assetName, bundleName);
        }

        public static void ExecuteAssetMove()
        {
            var size = m_moveData.Count;
            if (size > 0)
            {
                bool autoRefresh = EditorPrefs.GetBool("kAutoRefresh");
                EditorPrefs.SetBool("kAutoRefresh", false);
                EditorUtility.DisplayProgressBar("Moving assets to bundles", "", 0);
                for (int i = 0; i < size; i++)
                {
                    EditorUtility.DisplayProgressBar("Moving assets to bundle " + m_moveData[i].m_bundle, System.IO.Path.GetFileNameWithoutExtension(m_moveData[i].m_asset), (float)i / (float)size);
                    m_moveData[i].Apply();
                }
                EditorUtility.ClearProgressBar();
                EditorPrefs.SetBool("kAutoRefresh", autoRefresh);
                m_moveData.Clear();

                AssetDatabase.RemoveUnusedAssetBundleNames();
                Refresh();
            }
        }

        //public static AssetInfo GetAsset(string name)
        //{
        //    if (!name.StartsWith("Assets/"))
        //        return null;
        //    string ext = System.IO.Path.GetExtension(name);
        //    if (ext == ".dll" || ext == ".cs" || ext == ".meta")
        //        return null;


        //    AssetInfo info = null;
        //    if (!m_globalAssetList.TryGetValue(name, out info))
        //    {
        //        var bundleName = GetBundleName(name);
        //        info = new AssetInfo(name, bundleName);
        //        m_globalAssetList.Add(name, info);
        //    }
        //    return info;
        //}
        
        public static AssetInfo CreateAsset(string name, AssetInfo parent)
        {
            if (ValidateAsset(name))
            {
                var bundleName = GetBundleName(name);  //TODO - I don't think I want to call this here because I can't get here without bundle being "".
                return CreateAsset(name, bundleName, parent);
            }
            return null;
        }
        public static AssetInfo CreateAsset(string name, string bundleName)
        {
            if(ValidateAsset(name))
            {
                return CreateAsset(name, bundleName, null);
            }
            return null;
        }
        private static AssetInfo CreateAsset(string name, string bundleName, AssetInfo parent)
        {
            if(bundleName != string.Empty)
            {
                return new AssetInfo(name, bundleName);
            }
            else
            {
                AssetInfo info = null;
                if(!m_globalAssetList.TryGetValue(name, out info))
                {
                    info = new AssetInfo(name, string.Empty);
                    m_globalAssetList.Add(name, info);
                }
                info.AddParent(parent.DisplayName);
                return info;
            }

        }

        public static bool ValidateAsset(string name)
        {
            if (!name.StartsWith("Assets/"))
                return false;
            string ext = System.IO.Path.GetExtension(name);
            if (ext == ".dll" || ext == ".cs" || ext == ".meta")
                return false;

            return true;
        }


        internal static string GetBundleName(string asset)
        {
            var importer = AssetImporter.GetAtPath(asset);
            if (importer == null)
                return string.Empty;
            var bundleName = importer.assetBundleName;
            if (importer.assetBundleVariant.Length > 0)
                bundleName = bundleName + "." + importer.assetBundleVariant;
            return bundleName;
        }

        public static int RegisterAsset(AssetInfo asset, string bundle)
        {
            if(m_dependencyTracker.ContainsKey(asset.Name))
            {
                m_dependencyTracker[asset.Name].Add(bundle);
                int count = m_dependencyTracker[asset.Name].Count;
                if (count > 1)
                    asset.IsInMultipleBundles(true);
                return count;
            }

            var bundles = new HashSet<string>();
            bundles.Add(bundle);
            m_dependencyTracker.Add(asset.Name, bundles);
            return 1;            
        }
        public static void UnRegisterAsset(AssetInfo asset, string bundle)
        {
            if (m_dependencyTracker.ContainsKey(asset.Name))
            {
                m_dependencyTracker[asset.Name].Remove(bundle);
                int count = m_dependencyTracker[asset.Name].Count;
                switch (count)
                {
                    case 0:
                        m_dependencyTracker.Remove(asset.Name);
                        break;
                    case 1:
                        asset.IsInMultipleBundles(false);
                        break;
                    default:
                        break;
                }
            }
        }
        public static IEnumerable<string> CheckDependencyTracker(AssetInfo asset)
        {
            if (m_dependencyTracker.ContainsKey(asset.Name))
            {
                return m_dependencyTracker[asset.Name];
            }
            return new HashSet<string>();
        }
        public static void RemoveAsset(string asset)
        {
            m_dependencyTracker.Remove(asset);
        }

    }

    public class ProblemMessage
    {
        public enum Severity
        {
            None,
            Info,
            Warning,
            Error
        }

        public static Texture2D GetIcon(Severity sev)
        {
            if (sev == Severity.Error)
                return EditorGUIUtility.FindTexture("console.errorIcon");
            else if (sev == Severity.Warning)
                return EditorGUIUtility.FindTexture("console.warnicon");
            else if (sev == Severity.Info)
                return EditorGUIUtility.FindTexture("console.infoIcon");
            else
                return null;
        }

        public ProblemMessage(string msg, Severity sev)
        {
            message = msg;
            severity = sev;
        }

        public Severity severity;
        public string message;
        public Texture2D icon
        {
            get
            {
                return GetIcon(severity);
            }
        }
    }
}
