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
        public BundleTreeItem(BundleInfo b, int depth, string iconName) : base(b.NameHashCode, depth, b.DisplayName)
        {
            m_bundle = b;
            if (iconName == "")
                icon = null;
            else
                icon = EditorGUIUtility.FindTexture(iconName);

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
        public BundleNameData(string name) { SetName(name); }
        public BundleNameData(string path, string name)
        {
            string finalName = path == "" ? "" : path + '/';
            finalName += name;
            SetName(finalName);
        }

        public string Name
        {
            get { return m_fullName; }
            set { SetName(value); }
        }
        public string ShortName
        {
            get { return m_shortName; }
        }
        public List<string> NameTokens
        {
            get { return m_tokens; }
            set
            {
                m_tokens = value;
                GenerateFullName();
                m_shortName = m_tokens.Last();
            }
        }

        private void SetName(string name)
        {
            if(m_tokens == null)
            {
                m_tokens = new List<string>();
            }
            m_fullName = name;
            int indexOfSlash = name.IndexOf('/');
            int previousIndex = 0;
            while(indexOfSlash != -1)
            {
                m_tokens.Add(name.Substring(previousIndex, (indexOfSlash - previousIndex)));
                previousIndex = indexOfSlash + 1;
                indexOfSlash = name.IndexOf('/', previousIndex);
            }
            m_shortName = name.Substring(previousIndex);
            m_tokens.Add(m_shortName);
        }

        public void PartialNameChange(string newToken, int indexFromBack)
        {
            if(indexFromBack < m_tokens.Count)
            {
                m_tokens[m_tokens.Count - 1 - indexFromBack] = newToken;
                if(indexFromBack == 0)
                {
                    m_shortName = newToken;
                }
                GenerateFullName();
            }
        }

        private void GenerateFullName()
        {
            m_fullName = string.Empty;
            for(int i = 0; i < m_tokens.Count; i++)
            {
                m_fullName += m_tokens[i];
                if(i < m_tokens.Count - 1)
                {
                    m_fullName += '/';
                }
            }
        }

        string m_fullName;
        List<string> m_tokens;
        string m_shortName;
        
    }

    public abstract class BundleInfo
    {
        public BundleInfo(string name, BundleFolderInfo parent)
        {
            m_name = new BundleNameData(name);
            m_parent = parent;
        }

        protected BundleFolderInfo m_parent;
        protected bool m_doneUpdating;
        protected bool m_dirty;
        public BundleNameData m_name;

        public string DisplayName
        {
            get { return m_name.ShortName; }
        }
        public virtual int NameHashCode
        {
            get { return m_name.Name.GetHashCode(); }
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

        public virtual void HandleRename(string newName, int reverseDepth)
        {
            if(reverseDepth == 0)
            {
                m_parent.HandleChildRename(m_name.ShortName, newName);
            }
            m_name.PartialNameChange(newName, reverseDepth);
        }
        public virtual void HandleDelete(bool isRootOfDelete, string forcedNewName="")
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

        abstract public void HandleReparent(string parentName);
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
                AssetBundleModel.Model.UnRegisterAsset(asset, m_name.Name);
            }
        }
        public override void HandleRename(string newName, int reverseDepth)
        { 
            RefreshAssetList();
            base.HandleRename(newName, reverseDepth);
            Model.MoveAssetToBundle(m_concreteAssets, m_name.Name);
        }
        public override void HandleDelete(bool isRootOfDelete, string forcedNewName="")
        {
            RefreshAssetList();
            base.HandleDelete(isRootOfDelete);
            Model.MoveAssetToBundle(m_concreteAssets, forcedNewName);
        }

        private List<AssetInfo> m_concreteAssets;
        private List<AssetInfo> m_dependentAssets;
        private int m_concreteCounter;
        private int m_dependentCounter;
        private bool m_isSceneBundle;
        private long m_totalSize;

        public override void RefreshAssetList()
        {
            m_errorMessage = string.Empty;
            m_warningMessage = string.Empty;
            m_concreteAssets.Clear();
            m_totalSize = 0;
            m_isSceneBundle = false;
            var assets = AssetDatabase.GetAssetPathsFromAssetBundle(m_name.Name);
            foreach(var assetName in assets)
            {
                m_concreteAssets.Add(Model.CreateAsset(assetName, m_name.Name));
                m_totalSize += m_concreteAssets.Last().fileSize;
                if (AssetDatabase.GetMainAssetTypeAtPath(assetName) == typeof(SceneAsset))
                {
                    m_isSceneBundle = true;
                    m_concreteAssets.Last().IsScene = true;
                }
            }
            foreach(var asset in m_dependentAssets)
            {
                AssetBundleModel.Model.UnRegisterAsset(asset, m_name.Name);
            }
            m_dependentAssets.Clear();
            
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
                GatherDependencies(m_dependentAssets[m_dependentCounter]);
                m_dependentCounter++;
                m_doneUpdating = false;
            }
            else
            {
                m_doneUpdating = true;
            }
            m_dirty = dependents != m_dependentAssets.Count;
        }

        private void GatherDependencies(AssetInfo asset)
        {
            foreach (var ai in asset.GetDependencies())
            {
                if (ai == asset || m_concreteAssets.Contains(ai) || m_dependentAssets.Contains(ai))
                    continue;

                var bundleName = AssetDatabase.GetImplicitAssetBundleName(ai.Name);
                if (string.IsNullOrEmpty(bundleName))
                {
                    m_dependentAssets.Add(ai);
                    m_totalSize += ai.fileSize;
                    if (Model.RegisterAsset(ai, asset.BundleName) > 1)
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

        public override bool HasInfo()
        {
            return (m_concreteAssets.Count == 0);
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
            if(IsSceneBundle)
                return new BundleTreeItem(this, depth, "SceneAsset Icon");  
            else
                return new BundleTreeItem(this, depth, "CheckerFloor");
        }

        public override void HandleReparent(string parentName)
        {
            RefreshAssetList();
            string newName = (parentName=="") ? "" : parentName + '/';
            newName += DisplayName;
            if (newName == m_name.Name)
            {
                return;
            }
            foreach (var asset in m_concreteAssets)
            {
                Model.MoveAssetToBundle(asset, newName);
            }
        }

        public override List<AssetInfo> GetDependencies()
        {
            return m_dependentAssets;
        }
    }


    public class BundleFolderInfo : BundleInfo
    {
        public BundleFolderInfo(string name, BundleFolderInfo parent) : base(name, parent)
        {
            m_children = new Dictionary<string, BundleInfo>();
        }
        
        private Dictionary<string, BundleInfo> m_children;
        public BundleFolderInfo(List<string> path, int depth, BundleFolderInfo parent) : base("", parent)
        {
            m_children = new Dictionary<string, BundleInfo>();
            m_name = new BundleNameData("");
            m_name.NameTokens = path.GetRange(0, depth);
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

        public void AddChild(BundleInfo info)
        {
            m_children.Add(info.DisplayName, info);
        }

        public override BundleTreeItem CreateTreeView(int depth)
        {
            var result = new BundleTreeItem(this, depth, "Folder Icon");
            foreach (var child in m_children)
            {
                result.AddChild(child.Value.CreateTreeView(depth + 1));
            }
            return result;
        }

        public override void HandleRename(string newName, int reverseDepth)
        {
            base.HandleRename(newName, reverseDepth);
            foreach (var child in m_children)
            {
                child.Value.HandleRename(newName, reverseDepth + 1);
            }
        }
        public override void HandleDelete(bool isRootOfDelete, string forcedNewName="")
        {
            base.HandleDelete(isRootOfDelete);
            foreach (var child in m_children)
            {
                child.Value.HandleDelete(false, forcedNewName);
            }
            m_children.Clear();
        }

        public void HandleChildRename(string oldName, string newName)
        {
            BundleInfo info = null;
            if(m_children.TryGetValue(oldName, out info))
            {
                m_children.Remove(oldName);
                if(newName != string.Empty)
                    m_children.Add(newName, info);
            }
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
            return string.Empty;
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

        //public override List<BundleInfo> GetLeaves()
        //{
        //    List<BundleInfo> leaves = new List<BundleInfo>();
        //    foreach (var child in m_children)
        //    {
        //        leaves.AddRange(child.Value.GetLeaves());
        //    }
        //    return leaves;
        //}

        public override void HandleReparent(string parentName)
        {
            string newName = parentName + DisplayName;
            foreach (var child in m_children)
            {
                child.Value.HandleReparent(newName);
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


}
