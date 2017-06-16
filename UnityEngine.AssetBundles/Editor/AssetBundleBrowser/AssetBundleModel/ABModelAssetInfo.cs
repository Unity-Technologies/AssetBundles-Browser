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
        public AssetTreeItem(AssetInfo a) : base(a.fullAssetName.GetHashCode(), 0, a.displayName)
        {
            m_asset = a;
            icon = AssetDatabase.GetCachedIcon(a.fullAssetName) as Texture2D;
        }

        private Color m_color = new Color(0, 0, 0, 0);
        public Color itemColor
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
        public Texture2D MessageIcon()
        {
            return MessageSystem.GetIcon(HighestMessageLevel());
        }
        public MessageType HighestMessageLevel()
        {
            return m_asset.HighestMessageLevel();
        }

        public bool ContainsChild(AssetInfo asset)
        {
            bool contains = false;
            if (children == null)
                return contains;

            foreach(var child in children)
            {
                if( (child as AssetTreeItem).asset.fullAssetName == asset.fullAssetName )
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
        public bool isScene { get; set; }
        public bool isFolder { get; set; }
        public long fileSize;

        private HashSet<string> m_Parents;
        private string m_AssetName;
        private string m_DisplayName;
        private string m_BundleName;
        private MessageSystem.MessageState m_AssetMessages = new MessageSystem.MessageState();

        public AssetInfo(string inName, string bundleName="")
        {
            fullAssetName = inName;
            m_BundleName = bundleName;
            m_Parents = new HashSet<string>();
            isScene = false;
            isFolder = false;
        }

        public string fullAssetName
        {
            get { return m_AssetName; }
            set
            {
                m_AssetName = value;
                m_DisplayName = System.IO.Path.GetFileNameWithoutExtension(m_AssetName);

                //TODO - maybe there's a way to ask the AssetDatabase for this size info.
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(m_AssetName);
                if (fileInfo.Exists)
                    fileSize = fileInfo.Length;
                else
                    fileSize = 0;
            }
        }
        public string displayName
        {
            get { return m_DisplayName; }
        }
        public string bundleName
        { get { return m_BundleName == "" ? "auto" : m_BundleName; } }
        
        public Color GetColor()
        {
            if (m_BundleName == "")
                return Model.k_LightGrey;
            else
                return Color.white;
        }

        public bool IsMessageSet(MessageSystem.MessageFlag flag)
        {
            return m_AssetMessages.IsSet(flag);
        }
        public void SetMessageFlag(MessageSystem.MessageFlag flag, bool on)
        {
            m_AssetMessages.SetFlag(flag, on);
        }
        public MessageType HighestMessageLevel()
        {
            return m_AssetMessages.HighestMessageLevel();
        }
        public IEnumerable<MessageSystem.Message> GetMessages()
        {
            List<MessageSystem.Message> messages = new List<MessageSystem.Message>();
            if(IsMessageSet(MessageSystem.MessageFlag.SceneBundleConflict))
            {
                var message = displayName + "\n";
                if (isScene)
                    message += "Is a scene that is in a bundle with non-scene assets. Scene bundles must have only one or more scene assets.";
                else
                    message += "Is included in a bundle with a scene. Scene bundles must have only one or more scene assets.";
                messages.Add(new MessageSystem.Message(message, MessageType.Error));
            }
            if(IsMessageSet(MessageSystem.MessageFlag.DependencySceneConflict))
            {
                var message = displayName + "\n";
                message += MessageSystem.GetMessage(MessageSystem.MessageFlag.DependencySceneConflict).message;
                messages.Add(new MessageSystem.Message(message, MessageType.Error));
            }
            if (IsMessageSet(MessageSystem.MessageFlag.AssetsDuplicatedInMultBundles))
            {
                var bundleNames = AssetBundleModel.Model.CheckDependencyTracker(this);
                string message = displayName + "\n" + "Is auto-included in multiple bundles:\n";
                foreach(var bundleName in bundleNames)
                {
                    message += bundleName + ", ";
                }
                message = message.Substring(0, message.Length - 2);//remove trailing comma.
                messages.Add(new MessageSystem.Message(message, MessageType.Warning));
            }

            if (m_BundleName == string.Empty && m_Parents.Count > 0)
            {
                //TODO - refine the parent list to only include those in the current asset list
                var message = displayName + "\n" + "Is auto included in bundle(s) due to parent(s): \n";
                foreach (var parent in m_Parents)
                {
                    message += parent + ", ";
                }
                message = message.Substring(0, message.Length - 2);//remove trailing comma.
                messages.Add(new MessageSystem.Message(message, MessageType.Info));
            }

            messages.Add(new MessageSystem.Message(displayName + "\n" + "Path: " + fullAssetName, MessageType.Info));

            return messages;
        }
        public void AddParent(string name)
        {
            m_Parents.Add(name);
        }
        public void RemoveParent(string name)
        {
            m_Parents.Remove(name);
        }

        public string GetSizeString()
        {
            if (fileSize == 0)
                return "--";
            return EditorUtility.FormatBytes(fileSize);
        }

        List<AssetInfo> m_dependencies = null;
        public List<AssetInfo> GetDependencies()
        {
            //TODO - not sure this refreshes enough. need to build tests around that.
            if (m_dependencies == null)
            {
                m_dependencies = new List<AssetInfo>();
                if (AssetDatabase.IsValidFolder(m_AssetName))
                {
                    //if we have a folder, its dependencies were already pulled in through alternate means.  no need to GatherFoldersAndFiles
                    //GatherFoldersAndFiles();
                }
                else
                {
                    foreach (var dep in AssetDatabase.GetDependencies(m_AssetName, true))
                    {
                        if (dep != m_AssetName)
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
