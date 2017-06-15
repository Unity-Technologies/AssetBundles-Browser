using UnityEditor;

namespace UnityEngine.AssetBundles.AssetBundleDataSource
{
    public partial struct ABBuildInfo
    {
        public string outputDirectory;
        public BuildAssetBundleOptions options;
        public BuildTarget buildTarget;
    }

    public partial interface ABDataSource
    {
        //public static List<ABDataSource> CreateDataSources();
        string Name { get; }
        string ProviderName { get; }

        string[] GetAssetPathsFromAssetBundle (string assetBundleName);
        string GetAssetBundleName(string assetPath);
        string GetImplicitAssetBundleName(string assetPath);
        string[] GetAllAssetBundleNames();
        bool IsReadOnly();

        void SetAssetBundleNameAndVariant (string assetPath, string bundleName, string variantName);
        void RemoveUnusedAssetBundleNames();

        bool CanSpecifyBuildTarget { get; }
        bool CanSpecifyBuildOutputDirectory { get; }
        bool CanSpecifyBuildOptions { get; }

        bool BuildAssetBundles (ABBuildInfo info);
    }
}
