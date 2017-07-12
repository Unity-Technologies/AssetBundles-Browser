using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System;

namespace UnityEngine.AssetBundles
{
	public class InspectTreeItem : TreeViewItem
	{
		private string m_BundlePath;
        public string bundlePath
        {
            get { return m_BundlePath; }
        }
		private AssetBundle m_Bundle;

		private AssetBundleInspectTab m_InspectTab;
		//public InspectTreeItem(int id, int depth, string displayName) : base(id, depth, displayName)
		public InspectTreeItem(string path, AssetBundleInspectTab inspectTab) : base(path.GetHashCode(), 0, path)
		{
			m_BundlePath = path;
			m_Bundle = null;
			m_InspectTab = inspectTab;
		}
		public AssetBundle bundle
		{
			get
			{
                if (m_Bundle == null)
                    LoadBundle();
				return m_Bundle;
			}
		}
		public void LoadBundle()
		{
			if (m_Bundle == null)
			{
				m_Bundle = AssetBundle.LoadFromFile(m_BundlePath);
                m_InspectTab.SaveBundle(m_Bundle);

                //AssetBundleManifest manifest = m_Bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                //if (manifest != null)
                //{
                //    //this is where we could get some overall data. if we wanted it. which we might. someday.
                //}


                //gotta actually load assets to keep inspector from crashing :(
                var content = m_Bundle.GetAllAssetNames();
                foreach (var c in content)
                {
                    m_Bundle.LoadAsset(c);
                }
            }
		}
	}

	class InspectBundleTree : TreeView
	{
		AssetBundleInspectTab m_InspectTab;
		public InspectBundleTree(TreeViewState s, AssetBundleInspectTab parent) : base(s)
		{
			m_InspectTab = parent;
			showBorder = true;
		}

		protected override TreeViewItem BuildRoot()
		{
			var root = new TreeViewItem(-1, -1);
			root.children = new List<TreeViewItem>();
			if (m_InspectTab == null)
				Debug.Log("Unknown problem in AssetBundle Browser Inspect tab.  Restart Browser and try again, or file ticket on github.");
			else
			{
				foreach (var b in m_InspectTab.BundleList)
				{
					root.AddChild(new InspectTreeItem(b, m_InspectTab));
				}
			}
			return root;
		}

		public override void OnGUI(Rect rect)
		{
			base.OnGUI(rect);
			if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
			{
				SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
			}
		}

		protected override void SelectionChanged(IList<int> selectedIds)
		{
			base.SelectionChanged(selectedIds);
			
			if (selectedIds.Count > 0)
			{
				m_InspectTab.SetBundleItem(FindItem(selectedIds[0], rootItem) as InspectTreeItem);
			}
			else
				m_InspectTab.SetBundleItem(null);
		}

		protected override bool CanMultiSelect(TreeViewItem item)
		{
			return false;
		}
	}

}
