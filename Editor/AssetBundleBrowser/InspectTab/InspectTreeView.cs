using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using System;

namespace UnityEngine.AssetBundles
{
	public class InspectTreeItem : TreeViewItem
	{
        public string bundlePath { get; private set; }
            
		public InspectTreeItem(string path) : base(path.GetHashCode(), 0, path)
		{
            this.bundlePath = path;
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
					root.AddChild(new InspectTreeItem(b));
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
            {
				m_InspectTab.SetBundleItem(null);
            }
		}

		protected override bool CanMultiSelect(TreeViewItem item)
		{
			return false;
		}
	}

}
