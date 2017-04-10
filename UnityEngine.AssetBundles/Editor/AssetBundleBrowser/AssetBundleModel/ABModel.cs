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
        const string kNewVariantBaseName = "newvariant";
        public static /*const*/ Color kLightGrey = Color.grey * 1.5f;

        private static BundleFolderConcreteInfo m_rootLevelBundles = new BundleFolderConcreteInfo("", null);
        private static List<ABMoveData> m_moveData = new List<ABMoveData>();
        private static List<BundleInfo> m_bundlesToUpdate = new List<BundleInfo>();
        private static Dictionary<string, AssetInfo> m_globalAssetList = new Dictionary<string, AssetInfo>();
        private static Dictionary<string, HashSet<string>> m_dependencyTracker = new Dictionary<string, HashSet<string>>();

        private static bool m_inErrorState = false;
        const string kDefaultEmptyMessage = "Drag assets here or right-click to begin creating bundles.";
        const string kProblemEmptyMessage = "There was a problem parsing the list of bundles. See console.";
        private static string m_emptyMessageString;

        public static bool Update()
        {
            bool shouldRepaint = false;
            ExecuteAssetMove(false);     //this should never do anything. just a safety check.

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
            m_inErrorState = false;
            Rebuild();
            tree.Reload();
            bool doneUpdating = m_bundlesToUpdate.Count == 0;

            EditorUtility.DisplayProgressBar("Updating Bundles", "", 0);
            int fullBundleCount = m_bundlesToUpdate.Count;
            while (!doneUpdating && !m_inErrorState)
            {
                int currCount = m_bundlesToUpdate.Count;
                EditorUtility.DisplayProgressBar("Updating Bundles", m_bundlesToUpdate[currCount-1].DisplayName, (float)(fullBundleCount- currCount) / (float)fullBundleCount);
                doneUpdating = Update();
            }
            EditorUtility.ClearProgressBar();
        }
        public static void Rebuild()
        {
            m_rootLevelBundles = new BundleFolderConcreteInfo("", null);
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
            m_emptyMessageString = kProblemEmptyMessage;
            if (m_inErrorState)
                return;

            var bundleList = ValidateBundleList();
            if(bundleList != null)
            {
                m_emptyMessageString = kDefaultEmptyMessage;
                foreach (var bundleName in bundleList)
                {
                    AddBundleToModel(bundleName);
                }
                AddBundlesToUpdate(m_rootLevelBundles.GetChildList());
            }

            if(m_inErrorState)
            {
                m_rootLevelBundles = new BundleFolderConcreteInfo("", null);
                m_emptyMessageString = kProblemEmptyMessage;
            }
        }
        public static string[] ValidateBundleList()
        {
            var bundleList = AssetDatabase.GetAllAssetBundleNames();
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
                            AssetDatabase.RemoveUnusedAssetBundleNames();
                            index = 0;
                            bundleSet.Clear();
                            bundleList = AssetDatabase.GetAllAssetBundleNames();
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
                            AssetDatabase.RemoveUnusedAssetBundleNames();
                            index = 0;
                            bundleSet.Clear();
                            bundleList = AssetDatabase.GetAllAssetBundleNames();
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
            return (m_rootLevelBundles.GetChildList().Count() == 0);
        }

        public static string GetEmptyMessage()
        {
            return m_emptyMessageString;
        }

        public static BundleInfo CreateEmptyBundle(BundleFolderInfo folder = null, string newName = null)
        {
            folder = (folder == null) ? m_rootLevelBundles : folder;
            string name = GetUniqueName(folder, newName);
            BundleNameData nameData = new BundleNameData(folder.m_name.BundleName, name);
            return AddBundleToFolder(folder, nameData);
        }
        public static BundleInfo CreateEmptyVariant(BundleVariantFolderInfo folder)
        {
            string name = GetUniqueName(folder, kNewVariantBaseName);
            string variantName = folder.m_name.BundleName + "." + name;
            BundleNameData nameData = new BundleNameData(variantName);
            return AddBundleToFolder(folder.Parent, nameData);
        }
        public static BundleFolderInfo CreateEmptyBundleFolder(BundleFolderConcreteInfo folder = null)
        {
            folder = (folder == null) ? m_rootLevelBundles : folder;
            string name = GetUniqueName(folder) + "/dummy";
            BundleNameData nameData = new BundleNameData(folder.m_name.BundleName, name);
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
        private static BundleFolderConcreteInfo AddFoldersToBundle(BundleFolderInfo root, BundleNameData nameData)
        {
            BundleInfo currInfo = root;
            var folder = currInfo as BundleFolderConcreteInfo;
            var size = nameData.PathTokens.Count;
            for (var index = 0; index < size; index++)
            {
                if (folder != null)
                {
                    currInfo = folder.GetChild(nameData.PathTokens[index]);
                    if (currInfo == null)
                    {
                        currInfo = new BundleFolderConcreteInfo(nameData.PathTokens, index + 1, folder);
                        folder.AddChild(currInfo);
                    }

                    folder = currInfo as BundleFolderConcreteInfo;
                    if (folder == null)
                    {
                        m_inErrorState = true;
                        LogError("Bundle " + currInfo.m_name.FullNativeName + " has a name conflict with a bundle-folder.  Display of bundle data and building of bundles will not work.");
                        break;
                    }
                }
            }
            return currInfo as BundleFolderConcreteInfo;
        }
        private static BundleInfo AddBundleToFolder(BundleFolderInfo root, BundleNameData nameData)
        {
            BundleInfo currInfo = root.GetChild(nameData.ShortName);
            if (nameData.Variant != string.Empty)
            {
                if(currInfo == null)
                {
                    currInfo = new BundleVariantFolderInfo(nameData.BundleName, root);
                    root.AddChild(currInfo);
                }
                var folder = currInfo as BundleVariantFolderInfo;
                if (folder == null)
                {
                    var message = "Bundle named " + nameData.ShortName;
                    message += " exists both as a standard bundle, and a bundle with variants.  ";
                    message += "This message is not supported for display or actual bundle building.  ";
                    message += "You must manually fix bundle naming in the inspector.";
                    
                    LogError(message);
                    return null;
                }
                
                
                currInfo = folder.GetChild(nameData.Variant);
                if (currInfo == null)
                {
                    currInfo = new BundleVariantDataInfo(nameData.FullNativeName, folder);
                    folder.AddChild(currInfo);
                }
                
            }
            else
            {
                if (currInfo == null)
                {
                    currInfo = new BundleDataInfo(nameData.FullNativeName, root);
                    root.AddChild(currInfo);
                }
                else
                {
                    var dataInfo = currInfo as BundleDataInfo;
                    if (dataInfo == null)
                    {
                        m_inErrorState = true;
                        LogError("Bundle " + nameData.FullNativeName + " has a name conflict with a bundle-folder.  Display of bundle data and building of bundles will not work.");
                    }
                }
            }
            return currInfo;
        }

        private static string GetUniqueName(BundleFolderInfo folder, string suggestedName = null)
        {
            suggestedName = (suggestedName == null) ? kNewBundleBaseName : suggestedName;
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
            bool result = item.bundle.HandleRename(newName, 0);
            ExecuteAssetMove();
            return result;  
        }

        public static void HandleBundleReparent(IEnumerable<BundleInfo> bundles, BundleFolderInfo parent)
        {
            parent = (parent == null) ? m_rootLevelBundles : parent;
            foreach (var bundle in bundles)
            {
                bundle.HandleReparent(parent.m_name.BundleName, parent);
            }
            ExecuteAssetMove();
            //Rebuild();
        }

        public static void HandleBundleMerge(IEnumerable<BundleInfo> bundles, BundleDataInfo target)
        {
            foreach (var bundle in bundles)
            {
                bundle.HandleDelete(true, target.m_name.BundleName, target.m_name.Variant);
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

        public static BundleInfo HandleDedupeBundles(IEnumerable<BundleInfo> bundles, bool onlyOverlappedAssets)
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
                    if (onlyOverlappedAssets)
                    {
                        if (!fullAssetList.Add(asset.Name))
                            dupeAssets.Add(asset.Name);
                    }
                    else
                    {
                        if (asset.HasWarning())
                            dupeAssets.Add(asset.Name);
                    }
                }
            }

            if (dupeAssets.Count == 0)
                return null;
            
            MoveAssetToBundle(dupeAssets, newBundle.m_name.BundleName, string.Empty);
            ExecuteAssetMove();
            return newBundle;
        }

        public static BundleInfo ConvertToVariant(BundleDataInfo bundle)
        {
            bundle.HandleDelete(true, bundle.m_name.BundleName, kNewVariantBaseName);
            ExecuteAssetMove();
            var root = bundle.Parent.GetChild(bundle.m_name.ShortName) as BundleVariantFolderInfo;

            if (root != null)
                return root.GetChild(kNewVariantBaseName);
            else
            {
                //we got here because the converted bundle was empty.
                var vfolder = new BundleVariantFolderInfo(bundle.m_name.BundleName, bundle.Parent);
                var vdata = new BundleVariantDataInfo(bundle.m_name.BundleName + "." + kNewVariantBaseName, vfolder);
                bundle.Parent.AddChild(vfolder);
                vfolder.AddChild(vdata);
                return vdata;
            }
        }

        class ABMoveData
        {
            public string m_asset;
            public string m_bundle;
            public string m_variant;
            public ABMoveData(string asset, string bundle, string variant)
            {
                m_asset = asset;
                m_bundle = bundle;
                m_variant = variant;
            }
            public void Apply()
            {
                AssetImporter.GetAtPath(m_asset).SetAssetBundleNameAndVariant(m_bundle, m_variant);
            }
        }
        public static void MoveAssetToBundle(AssetInfo asset, string bundleName, string variant)
        {
            m_moveData.Add(new ABMoveData(asset.Name, bundleName, variant));
        }
        public static void MoveAssetToBundle(string assetName, string bundleName, string variant)
        {
            m_moveData.Add(new ABMoveData(assetName, bundleName, variant));
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
            var size = m_moveData.Count;
            if(forceAct)
            {
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
                }
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
            if (ext == ".dll" || ext == ".cs" || ext == ".meta" || ext == ".js" || ext == ".boo")
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


        static List<string> m_importedAssets = new List<string>();
        static List<string> m_deletedAssets = new List<string>();
        static List<KeyValuePair<string, string>> m_movedAssets = new List<KeyValuePair<string, string>>();
        class AssetBundleChangeListener : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                m_importedAssets.AddRange(importedAssets);
                m_deletedAssets.AddRange(deletedAssets);
                for (int i = 0; i < movedAssets.Length; i++)
                    m_movedAssets.Add(new KeyValuePair<string, string>(movedFromAssetPaths[i], movedAssets[i]));
                //m_dirty = true;
            }
        }

        static public void LogError(string message)
        {
            Debug.LogError("AssetBundleBrowser: " + message);
        }
        static public void LogWarning(string message)
        {
            Debug.LogWarning("AssetBundleBrowser: " + message);
        }

        static private Texture2D m_folderIcon = null;
        static private Texture2D m_bundleIcon = null;
        static private Texture2D m_sceneIcon = null;

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
