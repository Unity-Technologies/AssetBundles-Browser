using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

using UnityEngine.AssetBundles.AssetBundleDataSource;

namespace UnityEngine.AssetBundles.AssetBundleModel
{
    public class Model
    {
        const string k_NewBundleBaseName = "newbundle";
        const string k_NewVariantBaseName = "newvariant";
        public static /*const*/ Color k_LightGrey = Color.grey * 1.5f;

        private static ABDataSource m_DataSource;
        private static BundleFolderConcreteInfo m_RootLevelBundles = new BundleFolderConcreteInfo("", null);
        private static List<ABMoveData> m_MoveData = new List<ABMoveData>();
        private static List<BundleInfo> m_BundlesToUpdate = new List<BundleInfo>();
        private static Dictionary<string, AssetInfo> m_GlobalAssetList = new Dictionary<string, AssetInfo>();
        private static Dictionary<string, HashSet<string>> m_DependencyTracker = new Dictionary<string, HashSet<string>>();

        private static bool m_InErrorState = false;
        const string k_DefaultEmptyMessage = "Drag assets here or right-click to begin creating bundles.";
        const string k_ProblemEmptyMessage = "There was a problem parsing the list of bundles. See console.";
        private static string m_EmptyMessageString;

        static private Texture2D m_folderIcon = null;
        static private Texture2D m_bundleIcon = null;
        static private Texture2D m_sceneIcon = null;

        public static ABDataSource DataSource {
            get {
                if (m_DataSource == null) {
                    m_DataSource = new AssetDatabaseABDataSource ();
                }
                return m_DataSource;
            }
            set { m_DataSource = value; }
        }

        public static bool Update()
        {
            bool shouldRepaint = false;
            ExecuteAssetMove(false);     //this should never do anything. just a safety check.

            //TODO - look into EditorApplication callback functions.
            
            int size = m_BundlesToUpdate.Count;
            if (size > 0)
            {
                m_BundlesToUpdate[size - 1].Update();
                m_BundlesToUpdate.RemoveAll(item => item.doneUpdating == true);
                if (m_BundlesToUpdate.Count == 0)
                {
                    shouldRepaint = true;
                    foreach(var bundle in m_RootLevelBundles.GetChildList())
                    {
                        bundle.RefreshDupeAssetWarning();
                    }
                }
            }
            return shouldRepaint;
        }

        public static void ForceReloadData(TreeView tree)
        {
            m_InErrorState = false;
            Rebuild();
            tree.Reload();
            bool doneUpdating = m_BundlesToUpdate.Count == 0;

            EditorUtility.DisplayProgressBar("Updating Bundles", "", 0);
            int fullBundleCount = m_BundlesToUpdate.Count;
            while (!doneUpdating && !m_InErrorState)
            {
                int currCount = m_BundlesToUpdate.Count;
                EditorUtility.DisplayProgressBar("Updating Bundles", m_BundlesToUpdate[currCount-1].displayName, (float)(fullBundleCount- currCount) / (float)fullBundleCount);
                doneUpdating = Update();
            }
            EditorUtility.ClearProgressBar();
        }

        public static void Rebuild()
        {
            m_RootLevelBundles = new BundleFolderConcreteInfo("", null);
            m_MoveData = new List<ABMoveData>();
            m_BundlesToUpdate = new List<BundleInfo>();
            m_GlobalAssetList = new Dictionary<string, AssetInfo>();
            Refresh();
        }

        public static void AddBundlesToUpdate(IEnumerable<BundleInfo> bundles)
        {
            foreach(var bundle in bundles)
            {
                bundle.ForceNeedUpdate();
                m_BundlesToUpdate.Add(bundle);
            }
        }

        public static void Refresh()
        {
            m_EmptyMessageString = k_ProblemEmptyMessage;
            if (m_InErrorState)
                return;

            var bundleList = ValidateBundleList();
            if(bundleList != null)
            {
                m_EmptyMessageString = k_DefaultEmptyMessage;
                foreach (var bundleName in bundleList)
                {
                    AddBundleToModel(bundleName);
                }
                AddBundlesToUpdate(m_RootLevelBundles.GetChildList());
            }

            if(m_InErrorState)
            {
                m_RootLevelBundles = new BundleFolderConcreteInfo("", null);
                m_EmptyMessageString = k_ProblemEmptyMessage;
            }
        }

        public static string[] ValidateBundleList()
        {
            var bundleList = DataSource.GetAllAssetBundleNames();
            bool valid = true;
            HashSet<string> bundleSet = new HashSet<string>();
            int index = 0;
            bool attemptedBundleReset = false;
            while(index < bundleList.Length)
            {
                var name = bundleList[index];
                if (!bundleSet.Add(name))
                {
                    LogError("Two bundles share the same name: " + name);
                    valid = false;
                }

                int lastDot = name.LastIndexOf('.');
                if (lastDot > -1)
                {
                    var bunName = name.Substring(0, lastDot);
                    var extraDot = bunName.LastIndexOf('.');
                    if(extraDot > -1)
                    {
                        if(attemptedBundleReset)
                        {
                            var message = "Bundle name '" + bunName + "' contains a period.";
                            message += "  Internally Unity keeps 'bundleName' and 'variantName' separate, but externally treat them as 'bundleName.variantName'.";
                            message += "  If a bundleName contains a period, the build will (probably) succeed, but this tool cannot tell which portion is bundle and which portion is variant.";
                            LogError(message);
                            valid = false;
                        }
                        else
                        {
                            if (!DataSource.IsReadOnly ()) {
                                DataSource.RemoveUnusedAssetBundleNames();
                            }
                            index = 0;
                            bundleSet.Clear();
                            bundleList = DataSource.GetAllAssetBundleNames();
                            attemptedBundleReset = true;
                            continue;
                        }
                    }


                    if (bundleList.Contains(bunName))
                    {
                        //there is a bundle.none and a bundle.variant coexisting.  Need to fix that or return an error.
                        if (attemptedBundleReset)
                        {
                            valid = false;
                            var message = "Bundle name '" + bunName + "' exists without a variant as well as with variant '" + name.Substring(lastDot+1) + "'.";
                            message += " That is an illegal state that will not build and must be cleaned up.";
                            LogError(message);
                        }
                        else
                        {
                            if (!DataSource.IsReadOnly ()) {
                                DataSource.RemoveUnusedAssetBundleNames();
                            }
                            index = 0;
                            bundleSet.Clear();
                            bundleList = DataSource.GetAllAssetBundleNames();
                            attemptedBundleReset = true;
                            continue;
                        }
                    }
                }

                index++;
            }

            if (valid)
                return bundleList;
            else
                return null;
        }

        public static bool BundleListIsEmpty()
        {
            return (m_RootLevelBundles.GetChildList().Count() == 0);
        }

        public static string GetEmptyMessage()
        {
            return m_EmptyMessageString;
        }

        public static BundleInfo CreateEmptyBundle(BundleFolderInfo folder = null, string newName = null)
        {
            if ((folder as BundleVariantFolderInfo) != null)
                return CreateEmptyVariant(folder as BundleVariantFolderInfo);

            folder = (folder == null) ? m_RootLevelBundles : folder;
            string name = GetUniqueName(folder, newName);
            BundleNameData nameData;
            nameData = new BundleNameData(folder.m_Name.bundleName, name);
            return AddBundleToFolder(folder, nameData);
        }

        public static BundleInfo CreateEmptyVariant(BundleVariantFolderInfo folder)
        {
            string name = GetUniqueName(folder, k_NewVariantBaseName);
            string variantName = folder.m_Name.bundleName + "." + name;
            BundleNameData nameData = new BundleNameData(variantName);
            return AddBundleToFolder(folder.parent, nameData);
        }

        public static BundleFolderInfo CreateEmptyBundleFolder(BundleFolderConcreteInfo folder = null)
        {
            folder = (folder == null) ? m_RootLevelBundles : folder;
            string name = GetUniqueName(folder) + "/dummy";
            BundleNameData nameData = new BundleNameData(folder.m_Name.bundleName, name);
            return AddFoldersToBundle(m_RootLevelBundles, nameData);
        }

        private static BundleInfo AddBundleToModel(string name)
        {
            if (name == null)
                return null;
            
            BundleNameData nameData = new BundleNameData(name);

            BundleFolderInfo folder = AddFoldersToBundle(m_RootLevelBundles, nameData);
            BundleInfo currInfo = AddBundleToFolder(folder, nameData);

            return currInfo;
        }

        private static BundleFolderConcreteInfo AddFoldersToBundle(BundleFolderInfo root, BundleNameData nameData)
        {
            BundleInfo currInfo = root;
            var folder = currInfo as BundleFolderConcreteInfo;
            var size = nameData.pathTokens.Count;
            for (var index = 0; index < size; index++)
            {
                if (folder != null)
                {
                    currInfo = folder.GetChild(nameData.pathTokens[index]);
                    if (currInfo == null)
                    {
                        currInfo = new BundleFolderConcreteInfo(nameData.pathTokens, index + 1, folder);
                        folder.AddChild(currInfo);
                    }

                    folder = currInfo as BundleFolderConcreteInfo;
                    if (folder == null)
                    {
                        m_InErrorState = true;
                        LogError("Bundle " + currInfo.m_Name.fullNativeName + " has a name conflict with a bundle-folder.  Display of bundle data and building of bundles will not work.");
                        break;
                    }
                }
            }
            return currInfo as BundleFolderConcreteInfo;
        }

        private static BundleInfo AddBundleToFolder(BundleFolderInfo root, BundleNameData nameData)
        {
            BundleInfo currInfo = root.GetChild(nameData.shortName);
            if (nameData.variant != string.Empty)
            {
                if(currInfo == null)
                {
                    currInfo = new BundleVariantFolderInfo(nameData.bundleName, root);
                    root.AddChild(currInfo);
                }
                var folder = currInfo as BundleVariantFolderInfo;
                if (folder == null)
                {
                    var message = "Bundle named " + nameData.shortName;
                    message += " exists both as a standard bundle, and a bundle with variants.  ";
                    message += "This message is not supported for display or actual bundle building.  ";
                    message += "You must manually fix bundle naming in the inspector.";
                    
                    LogError(message);
                    return null;
                }
                
                
                currInfo = folder.GetChild(nameData.variant);
                if (currInfo == null)
                {
                    currInfo = new BundleVariantDataInfo(nameData.fullNativeName, folder);
                    folder.AddChild(currInfo);
                }
                
            }
            else
            {
                if (currInfo == null)
                {
                    currInfo = new BundleDataInfo(nameData.fullNativeName, root);
                    root.AddChild(currInfo);
                }
                else
                {
                    var dataInfo = currInfo as BundleDataInfo;
                    if (dataInfo == null)
                    {
                        m_InErrorState = true;
                        LogError("Bundle " + nameData.fullNativeName + " has a name conflict with a bundle-folder.  Display of bundle data and building of bundles will not work.");
                    }
                }
            }
            return currInfo;
        }

        private static string GetUniqueName(BundleFolderInfo folder, string suggestedName = null)
        {
            suggestedName = (suggestedName == null) ? k_NewBundleBaseName : suggestedName;
            string name = suggestedName;
            int index = 1;
            bool foundExisting = (folder.GetChild(name) != null);
            while (foundExisting)
            {
                name = suggestedName + index;
                index++;
                foundExisting = (folder.GetChild(name) != null);
            }
            return name;
        }

        public static BundleTreeItem CreateBundleTreeView()
        {
            return m_RootLevelBundles.CreateTreeView(-1);
        }

        public static AssetTreeItem CreateAssetListTreeView(IEnumerable<AssetBundleModel.BundleInfo> selectedBundles)
        {
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
            var originalName = new BundleNameData(item.bundle.m_Name.fullNativeName);

            var findDot = newName.LastIndexOf('.');
            var findSlash = newName.LastIndexOf('/');
            var findBSlash = newName.LastIndexOf('\\');
            if (findDot == 0 || findSlash == 0 || findBSlash == 0)
                return false; //can't start a bundle with a / or .

            bool result = item.bundle.HandleRename(newName, 0);

            if (findDot > 0 || findSlash > 0 || findBSlash > 0)
            {
                item.bundle.parent.HandleChildRename(newName, string.Empty);
            }

            ExecuteAssetMove();

            var node = FindBundle(originalName);
            if (node != null)
            {
                var message = "Failed to rename bundle named: ";
                message += originalName.fullNativeName;
                message += ".  Most likely this is due to the bundle being assigned to a folder in your Assets directory, AND that folder is either empty or only contains assets that are explicitly assigned elsewhere.";
                Debug.LogError(message);
            }

            return result;  
        }

        public static void HandleBundleReparent(IEnumerable<BundleInfo> bundles, BundleFolderInfo parent)
        {
            parent = (parent == null) ? m_RootLevelBundles : parent;
            foreach (var bundle in bundles)
            {
                bundle.HandleReparent(parent.m_Name.bundleName, parent);
            }
            ExecuteAssetMove();
        }

        public static void HandleBundleMerge(IEnumerable<BundleInfo> bundles, BundleDataInfo target)
        {
            foreach (var bundle in bundles)
            {
                bundle.HandleDelete(true, target.m_Name.bundleName, target.m_Name.variant);
            }
            ExecuteAssetMove();
        }

        public static void HandleBundleDelete(IEnumerable<BundleInfo> bundles)
        {
            var nameList = new List<BundleNameData>();
            foreach (var bundle in bundles)
            {
                nameList.Add(bundle.m_Name);
                bundle.HandleDelete(true);
            }
            ExecuteAssetMove();

            //check to see if any bundles are still there...
            foreach(var name in nameList)
            {
                var node = FindBundle(name);
                if(node != null)
                {
                    var message = "Failed to delete bundle named: ";
                    message += name.fullNativeName;
                    message += ".  Most likely this is due to the bundle being assigned to a folder in your Assets directory, AND that folder is either empty or only contains assets that are explicitly assigned elsewhere.";
                    Debug.LogError(message);
                }
            }
        }

        public static BundleInfo FindBundle(BundleNameData name)
        {
            BundleInfo currNode = m_RootLevelBundles;
            foreach (var token in name.pathTokens)
            {
                if(currNode is BundleFolderInfo)
                {
                    currNode = (currNode as BundleFolderInfo).GetChild(token);
                    if (currNode == null)
                        return null;
                }
                else
                {
                    return null;
                }
            }

            if(currNode is BundleFolderInfo)
            {
                currNode = (currNode as BundleFolderInfo).GetChild(name.shortName);
                if(currNode is BundleVariantFolderInfo)
                {
                    currNode = (currNode as BundleVariantFolderInfo).GetChild(name.variant);
                }
                return currNode;
            }
            else
            {
                return null;
            }
        }

        public static BundleInfo HandleDedupeBundles(IEnumerable<BundleInfo> bundles, bool onlyOverlappedAssets)
        {
            var newBundle = CreateEmptyBundle();
            HashSet<string> dupeAssets = new HashSet<string>();
            HashSet<string> fullAssetList = new HashSet<string>();

            //if they were just selected, then they may still be updating.
            bool doneUpdating = m_BundlesToUpdate.Count == 0;
            while (!doneUpdating)
                doneUpdating = Update();

            foreach (var bundle in bundles)
            {
                foreach (var asset in bundle.GetDependencies())
                {
                    if (onlyOverlappedAssets)
                    {
                        if (!fullAssetList.Add(asset.fullAssetName))
                            dupeAssets.Add(asset.fullAssetName);
                    }
                    else
                    {
                        if (asset.IsMessageSet(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles))
                            dupeAssets.Add(asset.fullAssetName);
                    }
                }
            }

            if (dupeAssets.Count == 0)
                return null;
            
            MoveAssetToBundle(dupeAssets, newBundle.m_Name.bundleName, string.Empty);
            ExecuteAssetMove();
            return newBundle;
        }

        public static BundleInfo HandleConvertToVariant(BundleDataInfo bundle)
        {
            bundle.HandleDelete(true, bundle.m_Name.bundleName, k_NewVariantBaseName);
            ExecuteAssetMove();
            var root = bundle.parent.GetChild(bundle.m_Name.shortName) as BundleVariantFolderInfo;

            if (root != null)
                return root.GetChild(k_NewVariantBaseName);
            else
            {
                //we got here because the converted bundle was empty.
                var vfolder = new BundleVariantFolderInfo(bundle.m_Name.bundleName, bundle.parent);
                var vdata = new BundleVariantDataInfo(bundle.m_Name.bundleName + "." + k_NewVariantBaseName, vfolder);
                bundle.parent.AddChild(vfolder);
                vfolder.AddChild(vdata);
                return vdata;
            }
        }

        class ABMoveData
        {
            public string assetName;
            public string bundleName;
            public string variantName;
            public ABMoveData(string asset, string bundle, string variant)
            {
                assetName = asset;
                bundleName = bundle;
                variantName = variant;
            }
            public void Apply()
            {
                if (!DataSource.IsReadOnly ()) {
                    DataSource.SetAssetBundleNameAndVariant(assetName, bundleName, variantName);
                }
            }
        }
        public static void MoveAssetToBundle(AssetInfo asset, string bundleName, string variant)
        {
            m_MoveData.Add(new ABMoveData(asset.fullAssetName, bundleName, variant));
        }
        public static void MoveAssetToBundle(string assetName, string bundleName, string variant)
        {
            m_MoveData.Add(new ABMoveData(assetName, bundleName, variant));
        }
        public static void MoveAssetToBundle(IEnumerable<AssetInfo> assets, string bundleName, string variant)
        {
            foreach (var asset in assets)
                MoveAssetToBundle(asset, bundleName, variant);
        }
        public static void MoveAssetToBundle(IEnumerable<string> assetNames, string bundleName, string variant)
        {
            foreach (var assetName in assetNames)
                MoveAssetToBundle(assetName, bundleName, variant);
        }

        public static void ExecuteAssetMove(bool forceAct=true)
        {
            var size = m_MoveData.Count;
            if(forceAct)
            {
                if (size > 0)
                {
                    bool autoRefresh = EditorPrefs.GetBool("kAutoRefresh");
                    EditorPrefs.SetBool("kAutoRefresh", false);
                    EditorUtility.DisplayProgressBar("Moving assets to bundles", "", 0);
                    for (int i = 0; i < size; i++)
                    {
                        EditorUtility.DisplayProgressBar("Moving assets to bundle " + m_MoveData[i].bundleName, System.IO.Path.GetFileNameWithoutExtension(m_MoveData[i].assetName), (float)i / (float)size);
                        m_MoveData[i].Apply();
                    }
                    EditorUtility.ClearProgressBar();
                    EditorPrefs.SetBool("kAutoRefresh", autoRefresh);
                    m_MoveData.Clear();
                }
                if (!DataSource.IsReadOnly ()) {
                    DataSource.RemoveUnusedAssetBundleNames();
                }
                Refresh();
            }
        }
        
        //this version of CreateAsset is only used for dependent assets. 
        public static AssetInfo CreateAsset(string name, AssetInfo parent)
        {
            if (ValidateAsset(name))
            {
                var bundleName = GetBundleName(name); 
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
                if(!m_GlobalAssetList.TryGetValue(name, out info))
                {
                    info = new AssetInfo(name, string.Empty);
                    m_GlobalAssetList.Add(name, info);
                }
                info.AddParent(parent.displayName);
                return info;
            }

        }

        public static bool ValidateAsset(string name)
        {
            if (!name.StartsWith("Assets/"))
                return false;
            string ext = System.IO.Path.GetExtension(name);
            if (ext == ".dll" || ext == ".cs" || ext == ".meta" || ext == ".js" || ext == ".boo")
                return false;

            return true;
        }

        internal static string GetBundleName(string asset)
        {
            return DataSource.GetAssetBundleName (asset);
        }

        public static int RegisterAsset(AssetInfo asset, string bundle)
        {
            if(m_DependencyTracker.ContainsKey(asset.fullAssetName))
            {
                m_DependencyTracker[asset.fullAssetName].Add(bundle);
                int count = m_DependencyTracker[asset.fullAssetName].Count;
                if (count > 1)
                    asset.SetMessageFlag(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles, true);
                return count;
            }

            var bundles = new HashSet<string>();
            bundles.Add(bundle);
            m_DependencyTracker.Add(asset.fullAssetName, bundles);
            return 1;            
        }

        public static void UnRegisterAsset(AssetInfo asset, string bundle)
        {
            if (m_DependencyTracker.ContainsKey(asset.fullAssetName))
            {
                m_DependencyTracker[asset.fullAssetName].Remove(bundle);
                int count = m_DependencyTracker[asset.fullAssetName].Count;
                switch (count)
                {
                    case 0:
                        m_DependencyTracker.Remove(asset.fullAssetName);
                        break;
                    case 1:
                        asset.SetMessageFlag(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles, false);
                        break;
                    default:
                        break;
                }
            }
        }

        public static IEnumerable<string> CheckDependencyTracker(AssetInfo asset)
        {
            if (m_DependencyTracker.ContainsKey(asset.fullAssetName))
            {
                return m_DependencyTracker[asset.fullAssetName];
            }
            return new HashSet<string>();
        }
        
        //TODO - switch local cache server on and utilize this method to stay up to date.
        //static List<string> m_importedAssets = new List<string>();
        //static List<string> m_deletedAssets = new List<string>();
        //static List<KeyValuePair<string, string>> m_movedAssets = new List<KeyValuePair<string, string>>();
        //class AssetBundleChangeListener : AssetPostprocessor
        //{
        //    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        //    {
        //        m_importedAssets.AddRange(importedAssets);
        //        m_deletedAssets.AddRange(deletedAssets);
        //        for (int i = 0; i < movedAssets.Length; i++)
        //            m_movedAssets.Add(new KeyValuePair<string, string>(movedFromAssetPaths[i], movedAssets[i]));
        //        //m_dirty = true;
        //    }
        //}

        static public void LogError(string message)
        {
            Debug.LogError("AssetBundleBrowser: " + message);
        }
        static public void LogWarning(string message)
        {
            Debug.LogWarning("AssetBundleBrowser: " + message);
        }

        static public Texture2D GetFolderIcon()
        {
            if (m_folderIcon == null)
                FindBundleIcons();
            return m_folderIcon;
        }
        static public Texture2D GetBundleIcon()
        {
            if (m_bundleIcon == null)
                FindBundleIcons();
            return m_bundleIcon;
        }
        static public Texture2D GetSceneIcon()
        {
            if (m_sceneIcon == null)
                FindBundleIcons();
            return m_sceneIcon;
        }
        static private void FindBundleIcons()
        {
            m_folderIcon = EditorGUIUtility.FindTexture("Folder Icon");
            string[] icons = AssetDatabase.FindAssets("ABundleBrowserIconY1756");
            foreach (string i in icons)
            {
                string name = AssetDatabase.GUIDToAssetPath(i);
                if (name.Contains("ABundleBrowserIconY1756Basic.png"))
                    m_bundleIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(name, typeof(Texture2D));
                else if (name.Contains("ABundleBrowserIconY1756Scene.png"))
                    m_sceneIcon = (Texture2D)AssetDatabase.LoadAssetAtPath(name, typeof(Texture2D));
            }
        }
    }
}
