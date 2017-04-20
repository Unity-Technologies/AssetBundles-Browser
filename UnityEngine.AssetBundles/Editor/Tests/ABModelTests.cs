using System;
using System.CodeDom;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Editor.Tests.Util;
using Boo.Lang.Runtime;
using UnityEngine.AssetBundles.AssetBundleModel;
using UnityEngine.SceneManagement;

public class ABModelTests
{
    private List<BundleInfo> m_BundleInfo;

    //TODO: Find stable way to remove instansiated objects in edit mode.  DestroyImmediate isn't working in this case


    /// <summary>
    /// Empty texutre for testing purposes
    /// </summary>
    Texture2D FakeTexture2D
    {
        get {return new Texture2D(16, 16);}
    }

    /// <summary>
    /// This is the Models root folder object.
    /// </summary>
    private BundleFolderConcreteInfo Root
    {
        get
        {
            FieldInfo rootFieldInfo = typeof(Model).GetField("m_RootLevelBundles",
            BindingFlags.NonPublic | BindingFlags.Static);
            BundleFolderConcreteInfo concreteFolder = rootFieldInfo.GetValue(null) as BundleFolderConcreteInfo;
            return concreteFolder;
        }
    }

    private List<BundleInfo> BundlesToUpdate
    {
        get
        {
            FieldInfo info = typeof(Model).GetField("m_BundlesToUpdate", BindingFlags.NonPublic | BindingFlags.Static);
            List<BundleInfo> bundleInfo = info.GetValue(null) as List<BundleInfo>;
            return bundleInfo;

        }
    }

    private IList MoveData
    {
        get
        {
            FieldInfo info = typeof(Model).GetField("m_MoveData", BindingFlags.NonPublic | BindingFlags.Static);
            var moveData = info.GetValue(null) as IList;
            return moveData;
        }
    }

    [SetUp]
    public void Setup()
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();

        Model.Rebuild();

        m_BundleInfo = new List<BundleInfo>();
        m_BundleInfo.Add(new BundleDataInfo("1bundle1", null));
        m_BundleInfo.Add(new BundleDataInfo("2bundle2", null));
        m_BundleInfo.Add(new BundleDataInfo("3bundle3", null));
    }

    [Test]
    public void AddBundlesToUpdate_AddsCorrectBundles_ToUpdateQueue()
    {
        Model.AddBundlesToUpdate(m_BundleInfo);
        Assert.AreEqual(3, BundlesToUpdate.Count);
    }

    [Test]
    public void Update_ReturnsTrue_ForRepaintOnFinalElement()
    {
        Model.AddBundlesToUpdate(m_BundleInfo);
        Assert.IsFalse(Model.Update());
        Assert.IsFalse(Model.Update());
        Assert.IsTrue(Model.Update());
    }

    [Test]
    public void ModelRebuild_Clears_BundlesToUpdate()
    {
        Model.AddBundlesToUpdate(m_BundleInfo);
        Model.Rebuild();
        Assert.AreEqual(0, BundlesToUpdate.Count);
    }

    [Test]
    public void UpdateShouldReturnFalseForRepaint()
    {
        Model.AddBundlesToUpdate(m_BundleInfo);
        Assert.IsFalse(Model.Update());
    }

    [Test]
    public void ValidateAssetBundleListIsZeroWhenNoBundleExist()
    {
        string[] list = Model.ValidateBundleList();
        Assert.AreEqual(0, list.Length);
    }

    [Test]
    public void ValidateAssetBundleList_ReturnsCorrect_ListOfBundles()
    {
        List<string> listOfPrefabs = new List<string>();

        //Arrange: Create a prefab and set it's asset bundle name
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundletest", String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            //Act: Operates on the list of asset bundle names found in the AssetDatabase
            string[] list = Model.ValidateBundleList();

            //Assert
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual("bundletest", list[0]);
        }, listOfPrefabs);
    }

    [Test]
    public void ValidateAssetBundleList_WithVariants_ContainsCorrectList()
    {
        List<string> listOfPrefabs = new List<string>();

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundletest", "v1"));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundletest", "v2"));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            //Act: Operates on the list of asset bundle names found in the AssetDatabase
            string[] list = Model.ValidateBundleList();

            //Assert
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual("bundletest.v1", list[0]);
            Assert.AreEqual("bundletest.v2", list[1]);
        }, listOfPrefabs);
    }

    [Test]
    public void ModelRebuild_KeepsCorrect_BundlesToUpdate()
    {
        List<string> listOfPrefabs = new List<string>();

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundletest", "v1"));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundletest", "v2"));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            Model.Rebuild();

            var rootChildList = Root.GetChildList();

            //Checks that the root has 1 bundle variant folder object as a child
            Assert.AreEqual(1, rootChildList.Count);
            Assert.AreEqual(typeof(BundleVariantFolderInfo), rootChildList.FirstOrDefault().GetType());

            BundleVariantFolderInfo folderInfo = rootChildList.FirstOrDefault() as BundleVariantFolderInfo;

            //Checks that the bundle variant folder object (mentioned above) has two children
            Assert.AreEqual(2, folderInfo.GetChildList().Count);

        }, listOfPrefabs);
    }

    [Test]
    public void VerifyBasicTreeStructure_ContainsCorrect_ClassTypes()
    {
        List<string> listOfPrefabs = new List<string>();

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundletest", "v1"));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundletest", "v2"));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            Model.Refresh();

            var rootChildList = Root.GetChildList();
            Assert.AreEqual(1, rootChildList.Count);
            Assert.AreEqual(typeof(BundleVariantFolderInfo), rootChildList.FirstOrDefault().GetType());

            BundleVariantFolderInfo folderInfo = rootChildList.FirstOrDefault() as BundleVariantFolderInfo;
            BundleInfo[] folderChildArray = folderInfo.GetChildList().ToArray();
            Assert.AreEqual(2, folderChildArray.Length);

            Assert.AreEqual(typeof(BundleVariantDataInfo), folderChildArray[0].GetType());
            Assert.AreEqual(typeof(BundleVariantDataInfo), folderChildArray[1].GetType());
        }, listOfPrefabs);

    }

    [Test]
    public void CreateEmptyBundle_AddsBundle_ToRootBundles()
    {
        Assert.AreEqual(0, GetBundleRootFolderChildCount());

        string bundleName = "testname";
        Model.CreateEmptyBundle(null, bundleName);

        Assert.AreEqual(1, GetBundleRootFolderChildCount());
    }

    [Test]
    public void CreatedEmptyBundle_Remains_AfterRefresh()
    {
        Assert.AreEqual(0, GetBundleRootFolderChildCount());

        //Arrange
        string bundleName = "testname";
        Model.CreateEmptyBundle(null, bundleName);

        //Act
        Model.Refresh();

        //Assert
        Assert.AreEqual(1, GetBundleRootFolderChildCount());
    }

    [Test]
    public void CreatedEmptyBundle_IsRemoved_AfterRebuild()
    {
        Assert.AreEqual(0, GetBundleRootFolderChildCount());

        string bundleName = "testname";
        Model.CreateEmptyBundle(null, bundleName);

        Model.Rebuild();

        Assert.AreEqual(0, GetBundleRootFolderChildCount());
    }

    [Test]
    public void MoveAssetToBundle_PlacesAsset_IntoMoveQueue()
    {
        string assetName = "New Asset";
        List<string> listOfPrefabs = new List<string>();

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundle1", String.Empty, assetName));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundle2", String.Empty, assetName));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            

            Assert.AreEqual(0, MoveData.Count);
            Model.MoveAssetToBundle(assetName, "bundle2", String.Empty);
            Assert.AreEqual(1, MoveData.Count);

        }, listOfPrefabs);
    }

    [Test]
    public void ExecuteAssetMove_MovesAssets_IntoCorrectBundles_UsingStrings()
    {
        List<string> listOfPrefabs = new List<string>();
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundle1", String.Empty, "Asset to Move"));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundle2", String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            Model.MoveAssetToBundle(listOfPrefabs[0], "bundle2", String.Empty);
            Model.ExecuteAssetMove();
            Assert.AreEqual("bundle2", AssetImporter.GetAtPath(listOfPrefabs[0]).assetBundleName);
            Assert.AreEqual(String.Empty, AssetImporter.GetAtPath(listOfPrefabs[0]).assetBundleVariant);

        }, listOfPrefabs);
    }

    [Test]
    public void ExecuteAssetMove_MovesAssets_IntoCorrectBundles_UsingAssetInfo()
    {
        List<string> listOfPrefabs = new List<string>();
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundle1", String.Empty, "Asset to Move"));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundle2", String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            AssetInfo info = Model.CreateAsset(listOfPrefabs[0], "bundle1");
            Model.MoveAssetToBundle(info, "bundle2", String.Empty);
            Model.ExecuteAssetMove();
            Assert.AreEqual("bundle2", AssetImporter.GetAtPath(listOfPrefabs[0]).assetBundleName);
            Assert.AreEqual(String.Empty, AssetImporter.GetAtPath(listOfPrefabs[0]).assetBundleVariant);

        }, listOfPrefabs);
    }

    [Test]
    public void CreateAsset_CreatesAsset_WithCorrectData()
    {
        string assetName = "Assets/assetName";
        string bunleName = "bundle1";

        AssetInfo info = Model.CreateAsset(assetName, bunleName);
        Assert.AreEqual(assetName, info.fullAssetName);
        Assert.AreEqual(bunleName, info.bundleName);
    }

    [Test]
    public void HandleBundleRename_RenamesTo_CorrectAssetBundleName()
    {
        BundleDataInfo dataInfo = new BundleDataInfo("bundledatainfo", Root);
        BundleTreeItem treeItem = new BundleTreeItem(dataInfo, 0, FakeTexture2D);
        
        bool handleBundle = Model.HandleBundleRename(treeItem, "newbundledatainfo");

        Assert.IsTrue(handleBundle);
        Assert.AreEqual(treeItem.bundle.m_Name.bundleName, "newbundledatainfo");
    }

    [Test]
    public void AssetBundleName_GetsRenamed_WhenBundleIsRenamed()
    {
        List<string> listOfPrefabs = new List<string>();
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundle1", String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            BundleInfo b = new BundleDataInfo("bundle1", Root);
            BundleTreeItem treeItem = new BundleTreeItem(b, 0, FakeTexture2D);

            Model.HandleBundleRename(treeItem, "bundle2");

            Assert.AreEqual("bundle2", AssetImporter.GetAtPath(listOfPrefabs[0]).assetBundleName);

        }, listOfPrefabs);
    }

    [Test]
    public void BundleFolderInfo_ChildrenTable_UpdatesWhenBundleIsRenamed()
    {
        List<string> listOfPrefabs = new List<string>();
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName("bundle1", String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            BundleInfo b = new BundleDataInfo("bundle1", Root);
            BundleTreeItem treeItem = new BundleTreeItem(b, 0, FakeTexture2D);
            Model.ExecuteAssetMove();

            Assert.AreEqual("bundle1", Root.GetChildList().ElementAt(0).m_Name.bundleName);
            Model.HandleBundleRename(treeItem, "bundle2");
            AssetDatabase.RemoveUnusedAssetBundleNames();
            Assert.AreEqual("bundle2", Root.GetChildList().ElementAt(0).m_Name.bundleName);

        }, listOfPrefabs);
    }

    [Test]
    public void BundleTreeItem_ChangesBundleName_AfterRename()
    {
        BundleInfo b = new BundleDataInfo("bundle1", Root);
        BundleTreeItem treeItem = new BundleTreeItem(b, 0, FakeTexture2D);
        Model.HandleBundleRename(treeItem, "bundle2");
        Assert.AreEqual("bundle2", treeItem.bundle.m_Name.bundleName);
    }

    int GetBundleRootFolderChildCount()
    {
        Dictionary<string, BundleInfo>.ValueCollection childList = Root.GetChildList();
        return childList.Count;
    }
}
