using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Linq;

namespace UnityEngine.AssetBundles
{
    public class Utilities
    {
        public static string kFolderIconName = "Folder";
        public static Texture2D FoldlerIcon { get { return EditorGUIUtility.FindTexture(Utilities.kFolderIconName) as Texture2D; } }
        public static T FindItem<T>(TreeViewItem root, int id) where T : TreeViewItem
        {
            if (!root.hasChildren)
                return null;
            for (int i = 0; i < root.children.Count; i++)
            {
                if (root.children[i].id == id)
                    return root.children[i] as T;
                var r = FindItem<T>(root.children[i], id);
                if (r != null)
                    return r;
            }
            return null;
        }



    }



    //TODO: not currently used, clean up for use
    public class AssetDependencyCache
    {
        public AssetInfo[] assets;

        public struct AssetInfo
        {
            public string name;         //full path name of asset: Assets/foo/bar.png
            public int[] dependencies;  //indices of dependencies
        }

        public AssetDependencyCache()
        {
            //find all assets
            string[] assetPaths = AssetDatabase.GetAllAssetPaths();
            assets = new AssetInfo[assetPaths.Length];
            for (int i = 0; i < assetPaths.Length; i++)
                assets[i].name = assetPaths[i];

            //find asset dependencies
            for (int i = 0; i < assets.Length; i++)
            {
                var filtered = AssetDatabase.GetDependencies(assets[i].name, false).Where(a => a != assets[i].name);
                assets[i].dependencies = new int[filtered.Count()];
                int di = 0;
                foreach (var d in filtered)
                    assets[i].dependencies[di++] = FindAssetIndex(d);
            }
        }

        int FindAssetIndex(string a)
        {
            for (int i = 0; i < assets.Length; i++)
                if (assets[i].name == a)
                    return i;
            return -1;
        }
        
        internal void CollectDependencies(int a, HashSet<int> deps)
        {
            var ai = assets[a];
            for (int i = 0; i < ai.dependencies.Length; i++)
            {
                if (deps.Add(ai.dependencies[i]))
                {
                    CollectDependencies(i, deps);
                }
            }
        }
    }

}
