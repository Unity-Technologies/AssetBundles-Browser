using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace UnityEngine.AssetBundles
{
    public class AssetBundleStateOld
    {
        public class BundleData
        {
            public string m_name = string.Empty;
            public BundleData m_parent = null;
            public List<BundleData> m_children = new List<BundleData>();
            public List<AssetData> m_assets = new List<AssetData>();
            public string fullName { get { return m_parent == null ? m_name : m_parent.fullName + (string.IsNullOrEmpty(m_parent.fullName) ? "" : "/") + m_name; } }
            public int depth { get { return m_parent == null ? -1 : m_parent.depth + 1; } }
            public int m_id;

            public BundleData(BundleData p, string n)
            {
                m_parent = p;
                m_name = n;
                m_id = m_name.GetHashCode();
                foreach (var a in AssetDatabase.GetAssetPathsFromAssetBundle(fullName))
                    m_assets.Add(GetAssetData(m_name, a));
            }

            public void MergeChildren(IEnumerable<string> mc)
            {
                BundleData fd = m_children.Find(s => s.m_name == mc.First());
                if (fd == null)
                    m_children.Add(fd = GetAssetBundle(this, mc.First()));
                if (mc.Count() > 1)
                    fd.MergeChildren(mc.Skip(1));
            }
        }

        public class AssetData
        {
            public int m_id;
            public string m_assetPath;
            public string m_displayName;
            public string m_bundle;
            public AssetData(string b, string a)
            {
                m_bundle = b;
                m_assetPath = a;
                m_id = m_assetPath.GetHashCode();
                m_displayName = System.IO.Path.GetFileNameWithoutExtension(m_assetPath);
            }
        }


        public static Dictionary<string, AssetData> s_assetDataMap = new Dictionary<string, AssetData>();
        public static Dictionary<string, BundleData> s_bundleDataMap = new Dictionary<string, BundleData>();
        public static BundleData s_bundleData;
        public static List<int> s_emptyIntList = new List<int>();
        public static AssetData GetAssetData(string bundle, string path)
        {
            AssetData data = null;
            if (s_assetDataMap.TryGetValue(path, out data))
                return data;
            data = new AssetData(bundle, path);
            s_assetDataMap.Add(path, data);
            return data;
        }

        public static BundleData GetAssetBundle(BundleData p, string name)
        {
            BundleData data = null;
            if (s_bundleDataMap.TryGetValue(name, out data))
                return data;
            data = new BundleData(p, name);
            s_bundleDataMap.Add(name, data);
            return data;
        }

        public static void InitializeBundleData(IEnumerable<AssetBundleBuild> abb)
        {
        }

        public static void InitializeBundleData(string[] bundleNames)
        {
            s_assetDataMap.Clear();
            s_bundleDataMap.Clear();
            AssetDependencyData dd = new AssetDependencyData();
            s_bundleData = new BundleData(null, "");
            foreach (var b in bundleNames)
                s_bundleData.MergeChildren(b.Split('/'));
        }

    }
}
