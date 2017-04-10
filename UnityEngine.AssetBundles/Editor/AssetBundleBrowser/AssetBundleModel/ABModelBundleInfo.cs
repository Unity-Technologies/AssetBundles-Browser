using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.AssetBundles.AssetBundleModel
{
    public class BundleTreeItem : TreeViewItem
    {
        private BundleInfo m_bundle;
        public BundleInfo bundle
        {
            get { return m_bundle; }
        }
        public BundleTreeItem(BundleInfo b, int depth, Texture2D iconTexture) : base(b.NameHashCode, depth, b.DisplayName)
        {
            m_bundle = b;
            icon = iconTexture;
            children = new List<TreeViewItem>();
        }


        //TODO - clean up this method. it's repeated elsewhere
        public Texture2D GetErrorIcon()
        {
            if(m_bundle.HasError())
            {
                return ProblemMessage.GetIcon(ProblemMessage.Severity.Error);
            }
            else if (m_bundle.HasWarning())
            {
                return ProblemMessage.GetIcon(ProblemMessage.Severity.Warning);
            }
            else if (m_bundle.HasInfo())
            {
                return ProblemMessage.GetIcon(ProblemMessage.Severity.Info);
            }
            return null;
        }
        public string ErrorMessage()
        {
            return m_bundle.ErrorMessage();
        }
    }

    public class BundleNameData
    {

        List<string> m_pathTokens;
        string m_fullBundleName;
        string m_shortName;
        string m_variantName;
        string m_fullNativeName;

        //input (received from native) is a string of format:
        //  /folder0/.../folderN/name.variant
        //it's broken into:
        //  /m_pathTokens[0]/.../m_pathTokens[n]/m_shortName.m_variantName
        // and...
        //  m_fullBundleName = /m_pathTokens[0]/.../m_pathTokens[n]/m_shortName
        // and...
        //  m_fullNativeName = m_fullBundleName.m_variantName which is the same as the initial input.
        public BundleNameData(string name) { SetName(name); }
        public BundleNameData(string path, string name)
        {
            string finalName = path == "" ? "" : path + '/';
            finalName += name;
            SetName(finalName);
        }
        public override int GetHashCode()
        {
            return FullNativeName.GetHashCode();
        }
        public string FullNativeName
        { get { return m_fullNativeName; } }

        public void SetBundleName(string bundleName, string variantName)
        {
            string name = bundleName;
            name += (variantName == "") ? "" : "." + variantName;
            SetName(name);
        }
        public string BundleName
        {
            get { return m_fullBundleName; }
            //set { SetName(value); }
        }
        public string ShortName
        {
            get { return m_shortName; }
        }
        public string Variant
        {
            get { return m_variantName; }
            set
            {
                m_variantName = value;
                m_fullNativeName = m_fullBundleName;
                m_fullNativeName += (m_variantName == "") ? "" : "." + m_variantName;
            }
        }
        public List<string> PathTokens
        {
            get { return m_pathTokens; }
            set
            {
                m_pathTokens = value.GetRange(0, value.Count-1);
                SetShortName(value.Last());
                GenerateFullName();
            }
        }

        private void SetName(string name)
        {
            if(m_pathTokens == null)
                m_pathTokens = new List<string>();
            else
                m_pathTokens.Clear();

            int indexOfSlash = name.IndexOf('/');
            int previousIndex = 0;
            while(indexOfSlash != -1)
            {
                m_pathTokens.Add(name.Substring(previousIndex, (indexOfSlash - previousIndex)));
                previousIndex = indexOfSlash + 1;
                indexOfSlash = name.IndexOf('/', previousIndex);
            }
            SetShortName(name.Substring(previousIndex));
            GenerateFullName();
        }
        private void SetShortName(string inputName)
        {
            m_shortName = inputName;
            int indexOfDot = m_shortName.LastIndexOf('.');
            if (indexOfDot > -1)
            {
                m_variantName = m_shortName.Substring(indexOfDot + 1);
                m_shortName = m_shortName.Substring(0, indexOfDot);
            }
            else
                m_variantName = string.Empty;
        }

        public void PartialNameChange(string newToken, int indexFromBack)
        {
            if(indexFromBack == 0)
            {
                 SetShortName(newToken);
            }
            else if(indexFromBack-1 < m_pathTokens.Count)
            {
                m_pathTokens[m_pathTokens.Count - indexFromBack] = newToken;
            }
            GenerateFullName();
        }

        private void GenerateFullName()
        {
            m_fullBundleName = string.Empty;
            for(int i = 0; i < m_pathTokens.Count; i++)
            {
                m_fullBundleName += m_pathTokens[i];
                m_fullBundleName += '/';
            }
            m_fullBundleName += m_shortName;
            m_fullNativeName = m_fullBundleName;
            m_fullNativeName += (m_variantName == "") ? "" : "." + m_variantName;
        }
    }

    public abstract class BundleInfo
    {
        public BundleInfo(string name, BundleFolderInfo parent)
        {
            m_name = new BundleNameData(name);
            m_parent = parent;
        }
        public BundleFolderInfo Parent
        { get { return m_parent; } }
        protected BundleFolderInfo m_parent;
        protected bool m_doneUpdating;
        protected bool m_dirty;
        public BundleNameData m_name;

        public virtual string DisplayName
        {
            get { return m_name.ShortName; }
        }
        public virtual int NameHashCode
        {
            get { return m_name.GetHashCode(); }
        }
        abstract public BundleTreeItem CreateTreeView(int depth);

        //protected bool m_error = false;
        //protected bool m_warning = false;
        protected string m_errorMessage = "";
        protected string m_warningMessage = "";
        protected virtual string InfoMessage { get { return ""; } }
        public virtual bool HasError() { return m_errorMessage != string.Empty; }
        public virtual bool HasWarning() { return m_warningMessage != string.Empty; }
        public abstract bool HasInfo();
        public abstract void RefreshWarning();
        public virtual string ErrorMessage()
        {
            if(HasError())
            {
                return m_errorMessage;
            }
            if(HasWarning())
            {
                return m_warningMessage;
            }
            if(HasInfo())
            {
                return InfoMessage;
            }
            return string.Empty;
        }

        public virtual bool HandleRename(string newName, int reverseDepth)
        {
            if (reverseDepth == 0)
            {
                if (!m_parent.HandleChildRename(m_name.ShortName, newName))
                    return false;
            }
            m_name.PartialNameChange(newName, reverseDepth);
            return true;
        }
        public virtual void HandleDelete(bool isRootOfDelete, string forcedNewName="", string forcedNewVariant = "")
        {
            if(isRootOfDelete)
            {
                m_parent.HandleChildRename(m_name.ShortName, string.Empty);
            }
        }
        abstract public void RefreshAssetList();
        abstract public void AddAssetsToNode(AssetTreeItem node);
        abstract public void Update();
        public virtual bool DoneUpdating
        { get { return m_doneUpdating; } }
        public virtual bool Dirty
        { get { return m_dirty; } }
        public void ForceNeedUpdate()
        {
            m_doneUpdating = false;
            m_dirty = true;
        }

        abstract public void HandleReparent(string parentName, BundleFolderInfo newParent = null);
        abstract public List<AssetInfo> GetDependencies();
    }

    public class BundleDataInfo : BundleInfo
    {
        public BundleDataInfo(string name, BundleFolderInfo parent) : base(name, parent)
        {
            m_concreteAssets = new List<AssetInfo>();
            m_dependentAssets = new List<AssetInfo>();
            m_concreteCounter = 0;
            m_dependentCounter = 0;
        }
        ~BundleDataInfo()
        {
            foreach (var asset in m_dependentAssets)
            {
                AssetBundleModel.Model.UnRegisterAsset(asset, m_name.FullNativeName);
            }
        }
        public override bool HandleRename(string newName, int reverseDepth)
        { 
            RefreshAssetList();
            if (!base.HandleRename(newName, reverseDepth))
                return false;
            Model.MoveAssetToBundle(m_concreteAssets, m_name.BundleName, m_name.Variant);
            return true;
        }
        public override void HandleDelete(bool isRootOfDelete, string forcedNewName="", string forcedNewVariant="")
        {
            RefreshAssetList();
            base.HandleDelete(isRootOfDelete);
            Model.MoveAssetToBundle(m_concreteAssets, forcedNewName, forcedNewVariant);
        }

        protected List<AssetInfo> m_concreteAssets;
        protected List<AssetInfo> m_dependentAssets;
        protected int m_concreteCounter;
        protected int m_dependentCounter;
        protected bool m_isSceneBundle;
        protected long m_totalSize;

        public override void RefreshAssetList()
        {
            m_errorMessage = string.Empty;
            m_warningMessage = string.Empty;
            m_concreteAssets.Clear();
            m_totalSize = 0;
            m_isSceneBundle = false;

            foreach (var asset in m_dependentAssets)
            {
                AssetBundleModel.Model.UnRegisterAsset(asset, m_name.FullNativeName);
            }
            m_dependentAssets.Clear();

            bool sceneInDependency = false;
            var assets = AssetDatabase.GetAssetPathsFromAssetBundle(m_name.FullNativeName);
            foreach(var assetName in assets)
            {
                var bundleName = Model.GetBundleName(assetName);
                if (bundleName == string.Empty)
                {
                    var partialPath = assetName;
                    while(
                        partialPath != string.Empty && 
                        partialPath != "Assets" &&
                        bundleName == string.Empty)
                    {
                        partialPath = partialPath.Substring(0, partialPath.LastIndexOf('/'));
                        bundleName = Model.GetBundleName(partialPath);
                    }
                    if(bundleName != string.Empty)
                    {
                        var folderAsset = Model.CreateAsset(partialPath, bundleName);
                        if (m_concreteAssets.FindIndex(a => a.DisplayName == folderAsset.DisplayName) == -1)
                            m_concreteAssets.Add(folderAsset);

                        m_dependentAssets.Add(Model.CreateAsset(assetName, folderAsset));

                        if (AssetDatabase.GetMainAssetTypeAtPath(assetName) == typeof(SceneAsset))
                        {
                            m_isSceneBundle = true;
                            if(sceneInDependency)
                            {
                                //we've hit more than one.  
                                m_errorMessage = "The folder added to this bundle has pulled in more than one scene which is not allowed.";
                            }
                            sceneInDependency = true;
                        }
                    }
                }
                else
                {
                    m_concreteAssets.Add(Model.CreateAsset(assetName, m_name.FullNativeName));
                    m_totalSize += m_concreteAssets.Last().fileSize;
                    if (AssetDatabase.GetMainAssetTypeAtPath(assetName) == typeof(SceneAsset))
                    {
                        m_isSceneBundle = true;
                        m_concreteAssets.Last().IsScene = true;
                    }
                }
            }
            
            if(IsSceneBundle && m_concreteAssets.Count > 1)
            {
                m_errorMessage = "A bundle with a scene must only contain that one scene.  This bundle has " + m_concreteAssets.Count + " explicitly added assets.";
                foreach (var asset in m_concreteAssets)
                {
                    asset.HasError(true);
                }
            }

            m_concreteCounter = 0;
            m_dependentCounter = 0;
            m_dirty = true;
        }
        public override void AddAssetsToNode(AssetTreeItem node)
        {
            foreach (var asset in m_concreteAssets)
                node.AddChild(new AssetTreeItem(asset));

            foreach (var asset in m_dependentAssets)
            {
                if(!node.ContainsChild(asset))
                    node.AddChild(new AssetTreeItem(asset));
            }

            m_dirty = false;
        }

        public override void Update()
        {
            int dependents = m_dependentAssets.Count;
            if(m_concreteCounter < m_concreteAssets.Count)
            {
                GatherDependencies(m_concreteAssets[m_concreteCounter]);
                m_concreteCounter++;
                m_doneUpdating = false;
            }
            else if (m_dependentCounter < m_dependentAssets.Count)
            {
                GatherDependencies(m_dependentAssets[m_dependentCounter], m_name.FullNativeName);
                m_dependentCounter++;
                m_doneUpdating = false;
            }
            else
            {
                m_doneUpdating = true;
            }
            m_dirty = dependents != m_dependentAssets.Count;
        }

        private void GatherDependencies(AssetInfo asset, string parentBundle = "")
        {
            if (parentBundle == string.Empty)
                parentBundle = asset.BundleName;

            foreach (var ai in asset.GetDependencies())
            {
                if (ai == asset || m_concreteAssets.Contains(ai) || m_dependentAssets.Contains(ai))
                    continue;

                var bundleName = AssetDatabase.GetImplicitAssetBundleName(ai.Name);
                if (string.IsNullOrEmpty(bundleName))
                {
                    m_dependentAssets.Add(ai);
                    m_totalSize += ai.fileSize;
                    if (Model.RegisterAsset(ai, parentBundle) > 1)
                    {
                        SetDuplicateWarning();
                    }
                }
            }
        }


        public override void RefreshWarning()
        {
            foreach(var asset in m_dependentAssets)
            {
                //this works right now because the only possible warning is duplicate assets.  If that changes, this will be invalid.
                if (asset.HasWarning()) 
                {
                    SetDuplicateWarning();
                    break;
                }
            }
        }

        public bool IsEmpty()
        {
            return (m_concreteAssets.Count == 0);
        }
        public override bool HasInfo()
        {
            return IsEmpty();
        }

        protected override string InfoMessage
        {
            get
            {
                if (HasInfo())
                    return "This bundle is empty.  Empty bundles cannot get saved with the scene and will disappear from this list if Unity restarts or if various other bundle rename or move events occur.";
                return "";
            }
        }

        protected void SetDuplicateWarning()
        {
            m_warningMessage = "Assets being pulled into this bundle due to dependencies are also being pulled into another bundle.";
            m_warningMessage += " This will cause duplication in memory";
        }

        public bool IsSceneBundle
        { get { return m_isSceneBundle; } }

        public override BundleTreeItem CreateTreeView(int depth)
        {
            RefreshAssetList();
            if (IsSceneBundle)
                return new BundleTreeItem(this, depth, Model.GetSceneIcon());
            else
                return new BundleTreeItem(this, depth, Model.GetBundleIcon());
        }

        public override void HandleReparent(string parentName, BundleFolderInfo newParent = null)
        {
            RefreshAssetList();
            string newName = (parentName=="") ? "" : parentName + '/';
            newName += m_name.ShortName;
            if (newName == m_name.BundleName)
                return;
            
            foreach (var asset in m_concreteAssets)
            {
                Model.MoveAssetToBundle(asset, newName, m_name.Variant);
            }

            if (newParent != null)
            {
                m_parent.HandleChildRename(m_name.ShortName, string.Empty);
                m_parent = newParent;
                m_parent.AddChild(this);
            }
            m_name.SetBundleName(newName, m_name.Variant);
        }

        public override List<AssetInfo> GetDependencies()
        {
            return m_dependentAssets;
        }
    }

    public class BundleVariantDataInfo : BundleDataInfo
    {
        public BundleVariantDataInfo(string name, BundleFolderInfo parent) : base(name, parent)
        {
        }
        ~BundleVariantDataInfo()
        {
            //parent should be auto called?
        }
        public override string DisplayName
        {
            get { return m_name.Variant; }
        }

        public bool IsSceneVariant()
        {
            RefreshAssetList();
            return IsSceneBundle;
        }
        public override bool HandleRename(string newName, int reverseDepth)
        {
            if (reverseDepth == 0)
            {
                RefreshAssetList();
                if (!m_parent.HandleChildRename(m_name.Variant, newName))
                    return false;
                m_name.Variant = newName;
                Model.MoveAssetToBundle(m_concreteAssets, m_name.BundleName, m_name.Variant);
            }
            else if (reverseDepth == 1)
            {
                RefreshAssetList();
                m_name.PartialNameChange(newName + "." + m_name.Variant, 0);
                Model.MoveAssetToBundle(m_concreteAssets, m_name.BundleName, m_name.Variant);
            }
            else
            {
                return base.HandleRename(newName, reverseDepth-1);
            }
            return true;
        }
        public override void HandleDelete(bool isRootOfDelete, string forcedNewName = "", string forcedNewVariant = "")
        {
            RefreshAssetList();
            if (isRootOfDelete)
            {
                m_parent.HandleChildRename(m_name.Variant, string.Empty);
            }
            Model.MoveAssetToBundle(m_concreteAssets, forcedNewName, forcedNewVariant);
        }
    }


    public abstract class BundleFolderInfo : BundleInfo
    {
        protected Dictionary<string, BundleInfo> m_children;

        public BundleFolderInfo(string name, BundleFolderInfo parent) : base(name, parent)
        {
            m_children = new Dictionary<string, BundleInfo>();
        }
        
        public BundleFolderInfo(List<string> path, int depth, BundleFolderInfo parent) : base("", parent)
        {
            m_children = new Dictionary<string, BundleInfo>();
            m_name = new BundleNameData("");
            m_name.PathTokens = path.GetRange(0, depth);
        }


        public BundleInfo GetChild(string name)
        {
            if (name == null)
                return null;

            BundleInfo info = null;
            if (m_children.TryGetValue(name, out info))
                return info;
            return null;
        }
        public Dictionary<string, BundleInfo>.ValueCollection GetChildList()
        {
            return m_children.Values;
        }
        public abstract void AddChild(BundleInfo info);


        public override bool HandleRename(string newName, int reverseDepth)
        {
            if (!base.HandleRename(newName, reverseDepth))
                return false;

            foreach (var child in m_children)
            {
                child.Value.HandleRename(newName, reverseDepth + 1);
            }
            return true;
        }

        public override void HandleDelete(bool isRootOfDelete, string forcedNewName="", string forcedNewVariant = "")
        {
            base.HandleDelete(isRootOfDelete);
            foreach (var child in m_children)
            {
                child.Value.HandleDelete(false, forcedNewName, forcedNewVariant);
            }
            m_children.Clear();
        }


        public override bool HasError()
        {
            bool error = false;
            foreach (var child in m_children)
            {
                error |= child.Value.HasError();
            }
            return error;
        }
        public override bool HasWarning()
        {
            bool warning = false;
            foreach (var child in m_children)
            {
                warning |= child.Value.HasWarning();
            }
            return warning;
        }
        public override bool HasInfo()
        {
            bool info = m_children.Count == 0;
            foreach (var child in m_children)
            {
                info |= child.Value.HasInfo();
            }
            return info;
        }
        protected override string InfoMessage
        {
            get
            {
                if (HasInfo())
                    return "This folder is either empty or contains only empty children.  Empty bundles cannot get saved with the scene and will disappear from this list if Unity restarts or if various other bundle rename or move events occur.";
                return "";
            }
        }
        public override string ErrorMessage()
        {
            if(HasError())
            {
                return "Error in child(ren)";
            }
            else if(HasWarning())
            {
                return "Warning in child(ren)";
            }
            return InfoMessage;
        }
        public override void RefreshAssetList()
        {
            foreach (var child in m_children)
            {
                child.Value.RefreshAssetList();
            }
        }
        public override void RefreshWarning()
        {
            foreach (var child in m_children)
            {
                child.Value.RefreshWarning();
            }
        }
        public override void AddAssetsToNode(AssetTreeItem node)
        {
            foreach (var child in m_children)
            {
                child.Value.AddAssetsToNode(node);
            }
            m_dirty = false;
        }
        public virtual bool HandleChildRename(string oldName, string newName)
        {

            if (newName != string.Empty && m_children.ContainsKey(newName))
            {
                Model.LogWarning("Attempting to name an item '" + newName + "' which matches existing name at this level in hierarchy.  If your desire is to merge bundles, drag one on top of the other.");
                return false;
            }

            BundleInfo info = null;
            if (m_children.TryGetValue(oldName, out info))
            {
                m_children.Remove(oldName);
                if (newName != string.Empty)
                    m_children.Add(newName, info);
            }
            return true;
        }

        public override void Update()
        {
            m_dirty = false;
            m_doneUpdating = true;
            foreach (var child in m_children)
            {
                child.Value.Update();
                m_dirty |= child.Value.Dirty;
                m_doneUpdating &= child.Value.DoneUpdating;
            }
        }
        public override bool DoneUpdating
        {
            get
            {
                foreach (var child in m_children)
                {
                    m_doneUpdating &= child.Value.DoneUpdating;
                }
                return base.DoneUpdating;
            }
        }


        public override List<AssetInfo> GetDependencies()
        {
            List<AssetInfo> assets = new List<AssetInfo>();
            foreach (var child in m_children)
            {
                assets.AddRange(child.Value.GetDependencies());
            }
            return assets;
        }
    }

    public class BundleFolderConcreteInfo : BundleFolderInfo
    {
        public BundleFolderConcreteInfo(string name, BundleFolderInfo parent) : base(name, parent)
        {
        }

        public BundleFolderConcreteInfo(List<string> path, int depth, BundleFolderInfo parent) : base(path, depth, parent)
        {
        }

        public override void AddChild(BundleInfo info)
        {
            m_children.Add(info.DisplayName, info);
        }
        public override BundleTreeItem CreateTreeView(int depth)
        {
            var result = new BundleTreeItem(this, depth, Model.GetFolderIcon());
            foreach (var child in m_children)
            {
                result.AddChild(child.Value.CreateTreeView(depth + 1));
            }
            return result;
        }
        public override void HandleReparent(string parentName, BundleFolderInfo newParent = null)
        {
            string newName = (parentName == "") ? "" : parentName + '/';
            newName += DisplayName;
            if (newName == m_name.BundleName)
                return;
            foreach (var child in m_children)
            {
                child.Value.HandleReparent(newName);
            }

            if (newParent != null)
            {
                m_parent.HandleChildRename(m_name.ShortName, string.Empty);
                m_parent = newParent;
                m_parent.AddChild(this);
            }
            m_name.SetBundleName(newName, m_name.Variant);
        }
    }
        public class BundleVariantFolderInfo : BundleFolderInfo
    {
        public BundleVariantFolderInfo(string name, BundleFolderInfo parent) : base(name, parent)
        {
        }
        public override void AddChild(BundleInfo info)
        {
            m_children.Add(info.m_name.Variant, info);
        }
        public override BundleTreeItem CreateTreeView(int depth)
        {
            Texture2D icon = null;
            if ((m_children.Count > 0) &&
                ((m_children.First().Value as BundleVariantDataInfo).IsSceneVariant()))
            {
                icon = Model.GetSceneIcon();
            }
            else
                icon = Model.GetBundleIcon();

            var result = new BundleTreeItem(this, depth, icon);
            foreach (var child in m_children)
            {
                result.AddChild(child.Value.CreateTreeView(depth + 1));
            }
            return result;
        }

        public override void HandleReparent(string parentName, BundleFolderInfo newParent = null)
        {
            string newName = (parentName == "") ? "" : parentName + '/';
            newName += DisplayName;
            if (newName == m_name.BundleName)
                return;
            foreach (var child in m_children)
            {
                child.Value.HandleReparent(parentName);
            }

            if (newParent != null)
            {
                m_parent.HandleChildRename(m_name.ShortName, string.Empty);
                m_parent = newParent;
                m_parent.AddChild(this);
            }
            m_name.SetBundleName(newName, string.Empty) ;
        }
        public override bool HandleChildRename(string oldName, string newName)
        {
            var result = base.HandleChildRename(oldName, newName);
            if (m_children.Count == 0)
                HandleDelete(true);
            return result;
        }
    }

}
