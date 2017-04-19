using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using System;


namespace UnityEngine.AssetBundles
{
    internal class BundleDetailItem : TreeViewItem
    {
        public BundleDetailItem(int id, int depth, string displayName, MessageType type) : base(id, depth, displayName)
        {
            MessageLevel = type;
        }

        public MessageType MessageLevel
        { get; set; }
    }
    internal class BundleDetailList : TreeView
    {
        HashSet<AssetBundleModel.BundleDataInfo> m_Selecteditems;
        Rect m_TotalRect;

        const float kDoubleIndent = 32f;
        const string kSizeHeader = "Size: ";
        const string kDependencyHeader = "Dependent On:";
        const string kDependencyEmpty = kDependencyHeader + " - None";
        const string kMessageHeader = "Messages:";
        const string kMessageEmpty = kMessageHeader + " - None";


        public BundleDetailList(TreeViewState state) : base(state)
        {
            m_Selecteditems = new HashSet<AssetBundleModel.BundleDataInfo>();
            showBorder = true;
        }
        public void Update()
        {
            bool dirty = false;
            foreach (var bundle in m_Selecteditems)
            {
                dirty |= bundle.Dirty;
            }
            if (dirty)
                Reload();
        }
        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            root.children = new List<TreeViewItem>();
            if (m_Selecteditems != null)
            {
                foreach(var bundle in m_Selecteditems)
                {
                    root.AddChild(AppendBundleToTree(bundle));
                }
            }
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if ((args.item as BundleDetailItem) != null)
            {
                EditorGUI.HelpBox(
                    new Rect(args.rowRect.x + kDoubleIndent, args.rowRect.y, args.rowRect.width - kDoubleIndent, args.rowRect.height), 
                    args.item.displayName,
                    (args.item as BundleDetailItem).MessageLevel);
            }
            else
            {
                Color old = GUI.color;
                if (args.item.depth == 1 &&
                    (args.item.displayName == kMessageEmpty || args.item.displayName == kDependencyEmpty))
                    GUI.color = AssetBundleModel.Model.kLightGrey;
                base.RowGUI(args);
                GUI.color = old;
            }
        }
        public override void OnGUI(Rect rect)
        {
            m_TotalRect = rect;
            base.OnGUI(rect);
        }
        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            if( (item as BundleDetailItem) != null)
            {
                float height = DefaultStyles.backgroundEven.CalcHeight(new GUIContent(item.displayName), m_TotalRect.width);
                return height + 3f;
            }
            return base.GetCustomRowHeight(row, item);
        }


        internal TreeViewItem AppendBundleToTree(AssetBundleModel.BundleDataInfo bundle)
        {
            var itemName = bundle.m_name.FullNativeName;
            var bunRoot = new TreeViewItem(itemName.GetHashCode(), 0, itemName);

            var str = itemName + kSizeHeader;
            var sz = new TreeViewItem(str.GetHashCode(), 1, kSizeHeader + bundle.TotalSize());

            str = itemName + kDependencyHeader;
            var dependency = new TreeViewItem(str.GetHashCode(), 1, kDependencyEmpty);
            var depList = bundle.GetBundleDependencies();
            if(depList.Count > 0)
            {
                dependency.displayName = kDependencyHeader;
                foreach (var dep in bundle.GetBundleDependencies())
                {
                    str = itemName + dep;
                    dependency.AddChild(new TreeViewItem(str.GetHashCode(), 2, dep));
                }
            }

            str = itemName + kMessageHeader;
            var msg = new TreeViewItem(str.GetHashCode(), 1, kMessageEmpty);
            if (bundle.HasMessages())
            {
                msg.displayName = kMessageHeader;
                var currMessages = bundle.GetMessages();

                foreach(var currMsg in currMessages)
                {
                    str = itemName + currMsg.message;
                    msg.AddChild(new BundleDetailItem(str.GetHashCode(), 2, currMsg.message, currMsg.severity));
                }
            }


            bunRoot.AddChild(sz);
            bunRoot.AddChild(dependency);
            bunRoot.AddChild(msg);

            return bunRoot;
        }



        internal void SetItems(IEnumerable<AssetBundleModel.BundleInfo> items)
        {
            m_Selecteditems.Clear();
            foreach(var item in items)
            {
                CollectBundles(item);
            }
            SetSelection(new List<int>());
            Reload();
        }
        internal void CollectBundles(AssetBundleModel.BundleInfo bundle)
        {
            var bunData = bundle as AssetBundleModel.BundleDataInfo;
            if (bunData != null)
                m_Selecteditems.Add(bunData);
            else
            {
                var bunFolder = bundle as AssetBundleModel.BundleFolderInfo;
                foreach (var bun in bunFolder.GetChildList())
                {
                    CollectBundles(bun);
                }
            }
        }

    }
}
