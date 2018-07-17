using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace AssetBundleBrowser
{
    internal class BundleDetailItem : TreeViewItem
    {
        internal BundleDetailItem(int id, int depth, string displayName, MessageType type) : base(id, depth, displayName)
        {
            MessageLevel = type;
        }

        internal MessageType MessageLevel
        { get; set; }
    }

    internal class TogglePathTreeViewItem : TreeViewItem
    {
        private static bool displayAlt = false;
        
        private string alternateDisplayName;
        
        public TogglePathTreeViewItem( int id, int depth, string displayName, string alternateDisplayName )
        {
            base.depth = depth;
            base.id = id;
            base.displayName = displayName;
            this.alternateDisplayName = alternateDisplayName;
        }
        
        public override string displayName
        {
            get
            {
                Event e = Event.current;
                if( e.alt && e.type == EventType.MouseDown )
                    displayAlt = !displayAlt;

                return displayAlt ? alternateDisplayName : base.displayName;
            }
            set
            {
                this.displayName = value;
            }
        }
    }
    internal class BundleDetailList : TreeView
    {
        HashSet<AssetBundleModel.BundleDataInfo> m_Selecteditems;
        Rect m_TotalRect;

        const float k_DoubleIndent = 32f;
        const string k_SizeHeader = "Size: ";
        const string k_DependencyHeader = "Dependent On:";
        const string k_DependencyEmpty = k_DependencyHeader + " - None";
        const string k_MessageHeader = "Messages:";
        const string k_MessageEmpty = k_MessageHeader + " - None";


        internal BundleDetailList(TreeViewState state) : base(state)
        {
            m_Selecteditems = new HashSet<AssetBundleModel.BundleDataInfo>();
            showBorder = true;
        }
        internal void Update()
        {
            bool dirty = false;
            foreach (var bundle in m_Selecteditems)
            {
                dirty |= bundle.dirty;
            }
            if (dirty)
            {
                Reload();
                ExpandAll( 2 );
            }
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
                    new Rect(args.rowRect.x + k_DoubleIndent, args.rowRect.y, args.rowRect.width - k_DoubleIndent, args.rowRect.height), 
                    args.item.displayName,
                    (args.item as BundleDetailItem).MessageLevel);
            }
            else
            {
                Color old = GUI.color;
                if (args.item.depth == 1 &&
                    (args.item.displayName == k_MessageEmpty || args.item.displayName == k_DependencyEmpty))
                    GUI.color = AssetBundleModel.Model.k_LightGrey;
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


        internal static TreeViewItem AppendBundleToTree(AssetBundleModel.BundleDataInfo bundle)
        {
            var itemName = bundle.m_Name.fullNativeName;
            var bunRoot = new TreeViewItem(itemName.GetHashCode(), 0, itemName);

            var str = itemName + k_SizeHeader;
            var sz = new TreeViewItem(str.GetHashCode(), 1, k_SizeHeader + bundle.TotalSize());

            str = itemName + k_DependencyHeader;
            var dependency = new TreeViewItem(str.GetHashCode(), 1, k_DependencyEmpty);
            var depList = bundle.GetBundleDependencies();
            if(depList.Count > 0)
            {
                dependency.displayName = k_DependencyHeader;
                foreach (var dep in bundle.GetBundleDependencies())
                {
                    str = itemName + dep.m_BundleName;
                    TreeViewItem newItem = new TreeViewItem( str.GetHashCode(), 2, dep.m_BundleName );
                    dependency.AddChild(newItem);
                    
                    Dictionary<string, TogglePathTreeViewItem> toAssetItems = new Dictionary<string, TogglePathTreeViewItem>();

                    for( int i = 0; i < dep.m_FromAssets.Count; ++i )
                    {
                        TogglePathTreeViewItem item = null;
                        
                        if( ! toAssetItems.TryGetValue( dep.m_ToAssets[i].fullAssetName, out item ) )
                        {
                            str = itemName + dep.m_BundleName + dep.m_ToAssets[i].displayName;
                            item = new TogglePathTreeViewItem( str.GetHashCode(), 3, dep.m_ToAssets[i].displayName, dep.m_ToAssets[i].fullAssetName );
                            newItem.AddChild( item );
                            toAssetItems.Add( dep.m_ToAssets[i].fullAssetName, item );
                        }

                        str = str + dep.m_FromAssets[i].displayName;
                        item.AddChild( new TogglePathTreeViewItem( str.GetHashCode(), 4,
                            string.Format( "Referenced by - {0}", dep.m_FromAssets[i].displayName ),
                            string.Format( "Referenced by - {0}", dep.m_FromAssets[i].fullAssetName ) ) );
                    }
                }
            }

            str = itemName + k_MessageHeader;
            var msg = new TreeViewItem(str.GetHashCode(), 1, k_MessageEmpty);
            if (bundle.HasMessages())
            {
                msg.displayName = k_MessageHeader;
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
            ExpandAll( 2 );
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

        internal void ExpandAll( int maximumDepth )
        {
            List<int> expanded = new List<int>( GetExpanded() );
            FindItems( rootItem, maximumDepth, expanded );
            SetExpanded( expanded );
        }
        
        internal void FindItems( TreeViewItem item, int maximumDepth, List<int> expanded )
        {
            if( item.depth >= maximumDepth || ! item.hasChildren )
                return;
            
            expanded.Add( item.id );
            for( int i = 0; i < item.children.Count; ++i )
            {
                FindItems( item.children[i], maximumDepth, expanded );
            }
        }
    }
}
