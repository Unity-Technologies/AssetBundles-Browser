using UnityEditor;

namespace AssetBundleBrowser.AssetBundleDataSource
{
    /// <summary>
    /// TODO - doc
    /// </summary>
    public partial struct ABBuildInfo
    {
        /// <summary>
        /// TODO - doc
        /// </summary>
        public string outputDirectory;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public BuildAssetBundleOptions options;
        /// <summary>
        /// TODO - doc
        /// </summary>
        public BuildTarget buildTarget;
    }

    /// <summary>
    /// TODO - doc
    /// Must implement CreateDataSources() to be picked up by the browser.
    /// </summary>
    public partial interface ABDataSource
    {
        //// all derived classes must implement the following interface in order to be picked up by the browser.
        //public static List<ABDataSource> CreateDataSources();

        /// <summary>
        /// TODO - doc
        /// </summary>
        string Name { get; }
        /// <summary>
        /// TODO - doc
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// TODO - doc
        /// </summary>
        string[] GetAssetPathsFromAssetBundle (string assetBundleName);
        /// <summary>
        /// TODO - doc
        /// </summary>
        string GetAssetBundleName(string assetPath);
        /// <summary>
        /// TODO - doc
        /// </summary>
        string GetImplicitAssetBundleName(string assetPath);
        /// <summary>
        /// TODO - doc
        /// </summary>
        string[] GetAllAssetBundleNames();
        /// <summary>
        /// TODO - doc
        /// </summary>
        bool IsReadOnly();

        /// <summary>
        /// TODO - doc
        /// </summary>
        void SetAssetBundleNameAndVariant (string assetPath, string bundleName, string variantName);
        /// <summary>
        /// TODO - doc
        /// </summary>
        void RemoveUnusedAssetBundleNames();

        /// <summary>
        /// TODO - doc
        /// </summary>
        bool CanSpecifyBuildTarget { get; }
        /// <summary>
        /// TODO - doc
        /// </summary>
        bool CanSpecifyBuildOutputDirectory { get; }
        /// <summary>
        /// TODO - doc
        /// </summary>
        bool CanSpecifyBuildOptions { get; }

        /// <summary>
        /// TODO - doc
        /// </summary>
        bool BuildAssetBundles (ABBuildInfo info);
    }
}
