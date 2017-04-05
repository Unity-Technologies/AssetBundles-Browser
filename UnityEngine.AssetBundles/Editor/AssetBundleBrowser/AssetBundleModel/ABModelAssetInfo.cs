using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.AssetBundles.AssetBundleModel
{
    public class AssetTreeItem : TreeViewItem
    {
        private AssetInfo m_asset;
        public AssetInfo asset
        {
            get { return m_asset; }
        }
        public AssetTreeItem() : base(-1, -1) { }
        public AssetTreeItem(AssetInfo a) : base(a.Name.GetHashCode(), 0, a.DisplayName)
        {
            m_asset = a;
            icon = AssetDatabase.GetCachedIcon(a.Name) as Texture2D;
        }

        private Color m_color = new Color(0, 0, 0, 0);
        public Color ItemColor
        {
            get
            {
                if (m_color.a == 0.0f)
                {
                    m_color = m_asset.GetColor();
                }
                return m_color;
            }
            set { m_color = value; }
        }

        public ProblemMessage.Severity HighestMessageLevel()
        {
            if (m_asset.HasError())
            { 
                return ProblemMessage.Severity.Error;
            }
            else if (m_asset.HasWarning())
            {
                return ProblemMessage.Severity.Warning;
            }
            return ProblemMessage.Severity.None;
        }

        public bool ContainsChild(AssetInfo asset)
        {
            bool contains = false;
            if (children == null)
                return contains;

            foreach(var child in children)
            {
                if( (child as AssetTreeItem).asset.Name == asset.Name )
                {
                    contains = true;
                    break;
                }
            }

            return contains;
        }


    }

    public class AssetInfo
    {
        public AssetInfo(string name, string bundleName="")
        {
            Name = name;
            m_bundleName = bundleName;
            m_parents = new HashSet<string>();
            IsScene = false;
        }
        //public AssetInfo(string name, AssetInfo parent)
        //{
        //    Name = name;
        //    m_bundleName = string.Empty;
        //    m_parent = parent;
        //}

        public bool IsScene { get; set; }
        private HashSet<string> m_parents;
        private string m_assetName;
        private string m_displayName;
        private string m_bundleName;
        public string Name
        {
            get { return m_assetName; }
            set
            {
                m_assetName = value;
                m_displayName = System.IO.Path.GetFileNameWithoutExtension(m_assetName);

                //TODO - maybe there's a way to ask the AssetDatabase for this size info.
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(m_assetName);
                if (fileInfo.Exists)
                    fileSize = fileInfo.Length;
                else
                    fileSize = 0;
            }
        }
        public string DisplayName
        {
            get { return m_displayName; }
        }
        public string BundleName
        { get { return m_bundleName == "" ? "auto" : m_bundleName; } }
        
        public Color GetColor()
        {
            if (m_bundleName == "")
                return Model.kLightGrey;
            else
                return Color.white;
        }

        private bool m_error = false;
        private bool m_warning = false;
        public bool HasError() { return m_error; }
        public void HasError(bool value) { m_error = value; }
        public bool HasWarning() { return m_warning; }
        public void IsInMultipleBundles(bool state) { m_warning = state; }
        public IEnumerable<ProblemMessage> GetMessages()
        {
            List<ProblemMessage> messages = new List<ProblemMessage>();
            if(HasError())
            {
                var message = DisplayName + "\n";
                if (IsScene)
                    message += "Is a scene that is in a bundle with other assets. Scene bundles must have a single scene as the only asset.";
                else
                    message += "Is included in a bundle with a scene. Scene bundles must have a single scene as the only asset.";
                messages.Add(new ProblemMessage(message, ProblemMessage.Severity.Error));
            }
            if (HasWarning())
            {
                var bundleNames = AssetBundleModel.Model.CheckDependencyTracker(this);
                string message = DisplayName + "\n" + "Is auto-included in multiple bundles:\n";
                foreach(var bundleName in bundleNames)
                {
                    message += bundleName + ", ";
                }
                message = message.Substring(0, message.Length - 2);//remove trailing comma.
                messages.Add(new ProblemMessage(message, ProblemMessage.Severity.Warning));
            }

            if (m_bundleName == string.Empty && m_parents.Count > 0)
            {
                //TODO - refine the parent list to only include those in the current asset list
                var message = DisplayName + "\n" + "Is auto included in bundle(s) due to parent(s): \n";
                foreach (var parent in m_parents)
                {
                    message += parent + ", ";
                }
                message = message.Substring(0, message.Length - 2);//remove trailing comma.
                messages.Add(new ProblemMessage(message, ProblemMessage.Severity.Info));
            }

            messages.Add(new ProblemMessage(DisplayName + "\n" + "Path: " + Name, ProblemMessage.Severity.Info));

            return messages;
        }
        public void AddParent(string name)
        {
            m_parents.Add(name);
        }
        public void RemoveParent(string name)
        {
            m_parents.Remove(name);
        }

        public long fileSize;
        public string GetSizeString()
        {
            if (fileSize == 0)
                return "--";
            return EditorUtility.FormatBytes(fileSize); ;
        }

        List<AssetInfo> m_dependencies = null;
        public List<AssetInfo> GetDependencies()
        {
            //TODO - not sure this refreshes enough. need to build tests around that.
            if (m_dependencies == null)
            {
                m_dependencies = new List<AssetInfo>();
                if (AssetDatabase.IsValidFolder(m_assetName))
                {
                    //if we have a folder, its dependencies were already pulled in through alternate means.  no need to GatherFoldersAndFiles

                    //GatherFoldersAndFiles();
                }
                else
                {
                    foreach (var dep in AssetDatabase.GetDependencies(m_assetName, true))
                    {
                        if (dep != m_assetName)
                        {
                            var asset = Model.CreateAsset(dep, this);
                            if (asset != null)
                                m_dependencies.Add(asset);
                        }
                    }
                }
            }
            return m_dependencies;
            
        }
    }
}
