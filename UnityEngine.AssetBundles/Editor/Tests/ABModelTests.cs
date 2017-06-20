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
using Assets.AssetBundles_Browser.UnityEngine.AssetBundles.Editor.Tests.Util;
using Assets.Editor.Tests.Util;
using Boo.Lang.Runtime;
using UnityEngine.AssetBundles.AssetBundleModel;
using UnityEngine.SceneManagement;

public class ABModelTests
{
    private List<BundleInfo> m_BundleInfo;

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

    [TearDown]
    public void TearDown()
    {
        GameObject[] gameObjectsInScene = GameObject.FindObjectsOfType<GameObject>()
            .Where(go => go.tag != "MainCamera").ToArray();

        foreach (GameObject obj in gameObjectsInScene)
        {
            GameObject.DestroyImmediate(obj, false);
        }
    }

    [Test]
    public void AddBundlesToUpdate_AddsCorrectBundles_ToUpdateQueue()
    {
        Model.AddBundlesToUpdate(m_BundleInfo);
        Assert.AreEqual(3, ABModelUtil.BundlesToUpdate.Count);
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
        Assert.AreEqual(0, ABModelUtil.BundlesToUpdate.Count);
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
        string bundleName = "bundletest";

        //Arrange: Create a prefab and set it's asset bundle name
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundleName, String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            //Act: Operates on the list of asset bundle names found in the AssetDatabase
            string[] list = Model.ValidateBundleList();

            //Assert
            Assert.AreEqual(1, list.Length);
            Assert.AreEqual(bundleName, list[0]);
        }, listOfPrefabs);
    }

    [Test]
    public void ValidateAssetBundleList_WithVariants_ContainsCorrectList()
    {
        List<string> listOfPrefabs = new List<string>();

        string bundleName = "bundletest";

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundleName, "v1"));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundleName, "v2"));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            //Act: Operates on the list of asset bundle names found in the AssetDatabase
            string[] list = Model.ValidateBundleList();

            //Assert
            Assert.AreEqual(2, list.Length);
            Assert.AreEqual(bundleName + ".v1", list[0]);
            Assert.AreEqual(bundleName + ".v2", list[1]);
        }, listOfPrefabs);
    }

    [Test]
    public void ModelRebuild_KeepsCorrect_BundlesToUpdate()
    {
        List<string> listOfPrefabs = new List<string>();

        string bundleName = "bundletest";

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundleName, "v1"));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundleName, "v2"));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            Model.Rebuild();

            var rootChildList = ABModelUtil.Root.GetChildList();

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
        string bundleName = "bundletest";

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundleName, "v1"));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundleName, "v2"));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            Model.Refresh();

            var rootChildList = ABModelUtil.Root.GetChildList();
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

        string bundle1Name = "bundle1";
        string bundle2Name = "bundle2";

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle1Name, String.Empty, assetName));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle2Name, String.Empty, assetName));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            

            Assert.AreEqual(0, ABModelUtil.MoveData.Count);
            Model.MoveAssetToBundle(assetName, bundle2Name, String.Empty);
            Assert.AreEqual(1, ABModelUtil.MoveData.Count);

        }, listOfPrefabs);
    }

    [Test]
    public void ExecuteAssetMove_MovesAssets_IntoCorrectBundles_UsingStrings()
    {
        List<string> listOfPrefabs = new List<string>();

        string bundle1Name = "bundle1";
        string bundle2Name = "bundle2";

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle1Name, String.Empty, "Asset to Move"));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle2Name, String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            Model.MoveAssetToBundle(listOfPrefabs[0], bundle2Name, String.Empty);
            Model.ExecuteAssetMove();
            Assert.AreEqual(bundle2Name, AssetImporter.GetAtPath(listOfPrefabs[0]).assetBundleName);
            Assert.AreEqual(String.Empty, AssetImporter.GetAtPath(listOfPrefabs[0]).assetBundleVariant);

        }, listOfPrefabs);
    }

    [Test]
    public void ExecuteAssetMove_MovesAssets_IntoCorrectBundles_UsingAssetInfo()
    {
        List<string> listOfPrefabs = new List<string>();

        string bundle1Name = "bundle1";
        string bundle2Name = "bundle2";

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle1Name, String.Empty, "Asset to Move"));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle2Name, String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            AssetInfo info = Model.CreateAsset(listOfPrefabs[0], bundle1Name);
            Model.MoveAssetToBundle(info, bundle2Name, String.Empty);
            Model.ExecuteAssetMove();
            Assert.AreEqual(bundle2Name, AssetImporter.GetAtPath(listOfPrefabs[0]).assetBundleName);
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
        string bundleDataInfoName = "bundledatainfo";
        string newBundleDataInfoName = "newbundledatainfo";

        BundleDataInfo dataInfo = new BundleDataInfo(bundleDataInfoName, ABModelUtil.Root);
        BundleTreeItem treeItem = new BundleTreeItem(dataInfo, 0, ABModelUtil.FakeTexture2D);
        
        bool handleBundle = Model.HandleBundleRename(treeItem, newBundleDataInfoName);

        Assert.IsTrue(handleBundle);
        Assert.AreEqual(treeItem.bundle.m_Name.bundleName, newBundleDataInfoName);
    }

    [Test]
    public void AssetBundleName_GetsRenamed_WhenBundleIsRenamed()
    {
        List<string> listOfPrefabs = new List<string>();

        string bundle1Name = "bundle1";
        string bundle2Name = "bundle2";

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle1Name, String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            BundleInfo b = new BundleDataInfo(bundle1Name, ABModelUtil.Root);
            BundleTreeItem treeItem = new BundleTreeItem(b, 0, ABModelUtil.FakeTexture2D);

            Model.HandleBundleRename(treeItem, bundle2Name);

            Assert.AreEqual(bundle2Name, AssetImporter.GetAtPath(listOfPrefabs[0]).assetBundleName);

        }, listOfPrefabs);
    }

    [Test]
    public void BundleFolderInfo_ChildrenTable_UpdatesWhenBundleIsRenamed()
    {
        List<string> listOfPrefabs = new List<string>();

        string bundle1Name = "bundle1";
        string bundle2Name = "bundle2";

        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle1Name, String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            BundleInfo b = new BundleDataInfo(bundle1Name, ABModelUtil.Root);
            ABModelUtil.Root.AddChild(b);
            BundleTreeItem treeItem = new BundleTreeItem(b, 0, ABModelUtil.FakeTexture2D);
            Model.ExecuteAssetMove();

            Assert.AreEqual(bundle1Name, ABModelUtil.Root.GetChildList().ElementAt(0).m_Name.bundleName);
            Model.HandleBundleRename(treeItem, bundle2Name);
            Assert.AreEqual(bundle2Name, ABModelUtil.Root.GetChildList().ElementAt(0).m_Name.bundleName);

        }, listOfPrefabs);
    }

    [Test]
    public void BundleTreeItem_ChangesBundleName_AfterRename()
    {
        string bundle1Name = "bundle1";
        string bundle2Name = "bundle2";

        BundleInfo b = new BundleDataInfo(bundle1Name, ABModelUtil.Root);
        BundleTreeItem treeItem = new BundleTreeItem(b, 0, ABModelUtil.FakeTexture2D);
        Model.HandleBundleRename(treeItem, bundle2Name);
        Assert.AreEqual(bundle2Name, treeItem.bundle.m_Name.bundleName);
    }

    [Test]
    public void HandleBundleReparent_MovesBundleDataInfoBundles_ToTheCorrectParent()
    {
        BundleDataInfo dataInfo = new BundleDataInfo("bundle1", ABModelUtil.Root);
        BundleFolderConcreteInfo concreteFolder = new BundleFolderConcreteInfo("folder1", ABModelUtil.Root);

        ABModelUtil.Root.AddChild(dataInfo);
        ABModelUtil.Root.AddChild(concreteFolder);

        Model.HandleBundleReparent(new BundleInfo[] {dataInfo}, concreteFolder);

        Assert.AreEqual(dataInfo.parent.m_Name.bundleName, concreteFolder.m_Name.bundleName);
    }

    [Test]
    public void HandleBundleReparent_MoveBundleFolderInfo_ToTheCorrectParent()
    {
        BundleFolderConcreteInfo concreteFolder = new BundleFolderConcreteInfo("folder1", ABModelUtil.Root);
        BundleFolderConcreteInfo subConcreteFolder = new BundleFolderConcreteInfo("subFolder1", concreteFolder);
        BundleFolderConcreteInfo folderToBeMoved = new BundleFolderConcreteInfo("folder2", subConcreteFolder);

        ABModelUtil.Root.AddChild(concreteFolder);
        concreteFolder.AddChild(subConcreteFolder);
        subConcreteFolder.AddChild(subConcreteFolder);

        Model.HandleBundleReparent(new BundleInfo[] {folderToBeMoved}, concreteFolder);

        Assert.AreEqual(concreteFolder.m_Name.bundleName, folderToBeMoved.parent.m_Name.bundleName);
    }

    [Test]
    public void HandleBundleReparent_MovesBundleVariant_ToCorrectParent()
    {
        BundleFolderConcreteInfo concreteFolder = Model.CreateEmptyBundleFolder() as BundleFolderConcreteInfo;
        BundleFolderConcreteInfo subConcreteFolder = Model.CreateEmptyBundleFolder(concreteFolder) as BundleFolderConcreteInfo;
        BundleFolderConcreteInfo startParent = Model.CreateEmptyBundleFolder(subConcreteFolder) as BundleFolderConcreteInfo;

        BundleVariantDataInfo bundleVariantDataInfo = new BundleVariantDataInfo("v1", startParent);

        Model.HandleBundleReparent(new BundleInfo[] {bundleVariantDataInfo}, concreteFolder);
        Assert.AreEqual(concreteFolder, bundleVariantDataInfo.parent);
    }

    [Test]
    public void HandleBundleReparent_MovesBundleFolderVariant_ToCorrectParent()
    {
        BundleFolderConcreteInfo concreteFolder = Model.CreateEmptyBundleFolder() as BundleFolderConcreteInfo;
        BundleFolderConcreteInfo startParent = Model.CreateEmptyBundleFolder() as BundleFolderConcreteInfo;
        BundleVariantDataInfo bundleVariantFolder = Model.CreateEmptyVariant(new BundleVariantFolderInfo("v1", startParent)) as BundleVariantDataInfo;
        
        Model.HandleBundleReparent(new BundleInfo[] { bundleVariantFolder }, concreteFolder);

        Assert.AreNotEqual(String.Empty, bundleVariantFolder.parent.m_Name.bundleName);
        Assert.AreEqual(concreteFolder, bundleVariantFolder.parent);
    }

    [Test]
    public void HandleBundleReparent_MovesBundle_IntoCorrectVariantFolder()
    {
        string variantFolderName = "variantfolder";
        string bundleName = "bundle1";

        BundleVariantFolderInfo bundleVariantFolderRoot = new BundleVariantFolderInfo(variantFolderName, ABModelUtil.Root);
        BundleDataInfo bundleDataInfo = new BundleDataInfo(bundleName, ABModelUtil.Root);

        ABModelUtil.Root.AddChild(bundleVariantFolderRoot);
        ABModelUtil.Root.AddChild(bundleDataInfo);

        Model.HandleBundleReparent(new BundleInfo[] {bundleDataInfo}, bundleVariantFolderRoot);

        Assert.AreEqual(variantFolderName + "/" + bundleName, bundleDataInfo.m_Name.bundleName);
    }

    [Test]
    public void HandleBundleDelete_Deletes_AllChildrenOfConcreteFolder()
    {
        BundleFolderConcreteInfo concreteFolder = new BundleFolderConcreteInfo("concreteFolder", ABModelUtil.Root);
        ABModelUtil.Root.AddChild(concreteFolder);

        BundleDataInfo bundleDataInfo1 = new BundleDataInfo("bundle1", concreteFolder);
        BundleDataInfo bundleDataInfo2 = new BundleDataInfo("bundle2", concreteFolder);
        BundleDataInfo bundleDataInfo3 = new BundleDataInfo("bundle3", concreteFolder);

        concreteFolder.AddChild(bundleDataInfo1);
        concreteFolder.AddChild(bundleDataInfo2);
        concreteFolder.AddChild(bundleDataInfo3);

        Model.HandleBundleDelete(new BundleInfo[] {concreteFolder});

        FieldInfo numberOfChildrenFieldInfo = typeof(BundleFolderConcreteInfo).GetField("m_Children", BindingFlags.NonPublic | BindingFlags.Instance);
        Dictionary<string, BundleInfo> numberOfConcreteFolderChildren =
            numberOfChildrenFieldInfo.GetValue(concreteFolder) as Dictionary<string, BundleInfo>;

        Assert.AreEqual(0, numberOfConcreteFolderChildren.Keys.Count);
    }

    [Test]
    public void HandleBundleDelete_Deletes_BundleDataInfo()
    {
        BundleDataInfo bundleDataInfo1 = new BundleDataInfo("bundle1", ABModelUtil.Root);
        BundleDataInfo bundleDataInfo2 = new BundleDataInfo("bundle2", ABModelUtil.Root);
        BundleDataInfo bundleDataInfo3 = new BundleDataInfo("bundle3", ABModelUtil.Root);

        ABModelUtil.Root.AddChild(bundleDataInfo1);
        ABModelUtil.Root.AddChild(bundleDataInfo2);
        ABModelUtil.Root.AddChild(bundleDataInfo3);

        Model.HandleBundleDelete(new BundleInfo[] { bundleDataInfo1, bundleDataInfo2, bundleDataInfo3});

        FieldInfo numberOfChildrenFieldInfo = typeof(BundleFolderConcreteInfo).GetField("m_Children", BindingFlags.NonPublic | BindingFlags.Instance);
        Dictionary<string, BundleInfo> numberOfConcreteFolderChildren =
            numberOfChildrenFieldInfo.GetValue(ABModelUtil.Root) as Dictionary<string, BundleInfo>;

        Assert.AreEqual(0, numberOfConcreteFolderChildren.Keys.Count);
    }

    [Test]
    public void HandleBundleDelete_Deletes_VariantFolderAndChildren()
    {
        BundleVariantFolderInfo bundleVariantFolderRoot = new BundleVariantFolderInfo("variantFolder", ABModelUtil.Root);
        ABModelUtil.Root.AddChild(bundleVariantFolderRoot);

        BundleVariantDataInfo bundleVariantDataInfo1 = new BundleVariantDataInfo("variant.a", bundleVariantFolderRoot);

        BundleVariantDataInfo bundleVariantDataInfo2 = new BundleVariantDataInfo("variant.b", bundleVariantFolderRoot);

        BundleVariantDataInfo bundleVariantDataInfo3 = new BundleVariantDataInfo("variant.c", bundleVariantFolderRoot);

        bundleVariantFolderRoot.AddChild(bundleVariantDataInfo1);
        bundleVariantFolderRoot.AddChild(bundleVariantDataInfo2);
        bundleVariantFolderRoot.AddChild(bundleVariantDataInfo3);

        FieldInfo numberOfChildrenFieldInfo = typeof(BundleFolderConcreteInfo).GetField("m_Children", BindingFlags.NonPublic | BindingFlags.Instance);
        Dictionary<string, BundleInfo> numberOfConcreteFolderChildren =
            numberOfChildrenFieldInfo.GetValue(ABModelUtil.Root) as Dictionary<string, BundleInfo>;

        Assert.AreEqual(1, numberOfConcreteFolderChildren.Keys.Count);

        Model.HandleBundleDelete(new BundleInfo[] { bundleVariantFolderRoot });

        numberOfConcreteFolderChildren =
            numberOfChildrenFieldInfo.GetValue(ABModelUtil.Root) as Dictionary<string, BundleInfo>;

        Assert.AreEqual(0, numberOfConcreteFolderChildren.Keys.Count);
    }

    [Test]
    public void HandleBundleDelete_Deletes_SingleVariantFromVariantFolder()
    {
        BundleVariantFolderInfo bundleVariantFolderRoot = new BundleVariantFolderInfo("variantFolder", ABModelUtil.Root);
        ABModelUtil.Root.AddChild(bundleVariantFolderRoot);

        BundleVariantDataInfo bundleVariantDataInfo1 = new BundleVariantDataInfo("variant1", bundleVariantFolderRoot);
        bundleVariantDataInfo1.m_Name.variant = "a";

        BundleVariantDataInfo bundleVariantDataInfo2 = new BundleVariantDataInfo("variant1", bundleVariantFolderRoot);
        bundleVariantDataInfo2.m_Name.variant = "b";

        BundleVariantDataInfo bundleVariantDataInfo3 = new BundleVariantDataInfo("variant1", bundleVariantFolderRoot);
        bundleVariantDataInfo3.m_Name.variant = "c";

        bundleVariantFolderRoot.AddChild(bundleVariantDataInfo1);
        bundleVariantFolderRoot.AddChild(bundleVariantDataInfo2);
        bundleVariantFolderRoot.AddChild(bundleVariantDataInfo3);

        FieldInfo numberOfChildrenFieldInfo = typeof(BundleFolderConcreteInfo).GetField("m_Children", BindingFlags.NonPublic | BindingFlags.Instance);
        Dictionary<string, BundleInfo> numberOfConcreteFolderChildren =
            numberOfChildrenFieldInfo.GetValue(ABModelUtil.Root) as Dictionary<string, BundleInfo>;

        Assert.AreEqual(1, numberOfConcreteFolderChildren.Keys.Count);

        Model.HandleBundleDelete(new BundleInfo[] { bundleVariantDataInfo1 });

        numberOfConcreteFolderChildren =
            numberOfChildrenFieldInfo.GetValue(ABModelUtil.Root) as Dictionary<string, BundleInfo>;

        Assert.AreEqual(1, numberOfConcreteFolderChildren.Keys.Count);

        FieldInfo numberOfVariantFolderChildrenFieldInfo = typeof(BundleVariantFolderInfo).GetField("m_Children", BindingFlags.NonPublic | BindingFlags.Instance);
        Dictionary<string, BundleInfo> numberOfVariantFolderChildren =
            numberOfVariantFolderChildrenFieldInfo.GetValue(bundleVariantFolderRoot) as Dictionary<string, BundleInfo>;

        Assert.AreEqual(2, numberOfVariantFolderChildren.Keys.Count);
    }

    [Test]
    public void HandleBundleMerge_Merges_BundlesCorrectly()
    {
        string bundle1Name = "bundle1";
        string bundle2Name = "bundle2";

        BundleDataInfo bundle1DataInfo = Model.CreateEmptyBundle() as BundleDataInfo;
        Model.HandleBundleRename(new BundleTreeItem(bundle1DataInfo, 0, ABModelUtil.FakeTexture2D), bundle1Name);

        BundleDataInfo bundle2DataInfo = Model.CreateEmptyBundle() as BundleDataInfo;
        Model.HandleBundleRename(new BundleTreeItem(bundle2DataInfo, 0, ABModelUtil.FakeTexture2D), bundle2Name);

        List<string> listOfPrefabs = new List<string>();
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle1Name, String.Empty));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle2Name, String.Empty));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle2Name, String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            Model.HandleBundleMerge(new BundleInfo[] {bundle2DataInfo}, bundle1DataInfo);

            Assert.AreEqual(1, AssetDatabase.GetAllAssetBundleNames().Length);
            Assert.AreEqual(bundle1Name, AssetDatabase.GetAllAssetBundleNames()[0]);

            //Make sure every asset now has bundle1 as the bundle name
            foreach (string prefab in listOfPrefabs)
            {
                Assert.AreEqual(bundle1Name, AssetImporter.GetAtPath(prefab).assetBundleName);
            }

        }, listOfPrefabs);
    }

    [Test]
    public void HandleBundleMerge_Merges_BundlesWithChildrenCorrectly()
    {
        string folderName = "folder";
        string bundle1Name = "bundle1";
        string bundle2Name = folderName + "/bundle2";

        BundleFolderConcreteInfo concrete = new BundleFolderConcreteInfo(folderName, ABModelUtil.Root);
        BundleDataInfo bundle1DataInfo = new BundleDataInfo(bundle1Name, ABModelUtil.Root);
        BundleDataInfo bundle2DataInfo = new BundleDataInfo(bundle2Name, concrete); 
        concrete.AddChild(bundle2DataInfo);

        List<string> listOfPrefabs = new List<string>();
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle1Name, String.Empty));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle2Name, String.Empty));
        listOfPrefabs.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle2Name, String.Empty));

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            Model.HandleBundleMerge(new BundleInfo[] { bundle1DataInfo }, bundle2DataInfo);

            Assert.AreEqual(1, AssetDatabase.GetAllAssetBundleNames().Length, GetAllElementsAsString(AssetDatabase.GetAllAssetBundleNames()));
            Assert.AreEqual(bundle2Name, AssetDatabase.GetAllAssetBundleNames()[0]);

            //Make sure every asset now has bundle1 as the bundle name
            foreach (string prefab in listOfPrefabs)
            {
                Assert.AreEqual(bundle2Name, AssetImporter.GetAtPath(prefab).assetBundleName);
            }

        }, listOfPrefabs);
    }

    [Test]
    public void HandleConvertToVariant_Converts_BundlesToVariant()
    {
        BundleInfo dataInfo = new BundleDataInfo("folder", ABModelUtil.Root);
        dataInfo = Model.HandleConvertToVariant((BundleDataInfo) dataInfo);
        Assert.AreEqual(typeof(BundleVariantDataInfo), dataInfo.GetType());
    }

    [Test]
    public void HandleDedupeBundles_MovesDuplicatedAssets_ToNewBundle()
    {
        string bundle1PrefabInstanceName = "Bundle1Prefab";
        string bundle2PrefabInstanceName = "Bundle2Prefab";

        string bundle1Name = "bundle1";
        string bundle2Name = "bundle2";

        List<string> listOfAssets = new List<string>();
        listOfAssets.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle1Name, "", bundle1PrefabInstanceName));
        listOfAssets.Add(TestUtil.CreatePrefabWithBundleAndVariantName(bundle2Name, "", bundle2PrefabInstanceName));

        BundleDataInfo bundle1DataInfo = new BundleDataInfo(bundle1Name, ABModelUtil.Root);
        BundleDataInfo bundle2DataInfo = new BundleDataInfo(bundle2Name, ABModelUtil.Root);

        ABModelUtil.Root.AddChild(bundle1DataInfo);
        ABModelUtil.Root.AddChild(bundle2DataInfo);

        bundle1DataInfo.RefreshAssetList();
        bundle2DataInfo.RefreshAssetList();

        //Need a material with no assigned bundle so it'll be pulled into both bundles
        string materialPath = "Assets/material.mat";
        Material mat = new Material(Shader.Find("Diffuse"));
        AssetDatabase.CreateAsset(mat, materialPath);
        listOfAssets.Add(materialPath);
        //

        Model.Refresh();

        TestUtil.ExecuteCodeAndCleanupAssets(() =>
        {
            AddMaterialsToMultipleObjects(new string[] { bundle1PrefabInstanceName, bundle2PrefabInstanceName }, listOfAssets, mat);
            Model.HandleDedupeBundles(new BundleInfo[] { bundle1DataInfo, bundle2DataInfo }, false);
            //This checks to make sure that a newbundle was automatically created since we dont' set this up anywhere else.
            Assert.IsTrue(AssetDatabase.GetAllAssetBundleNames().Contains("newbundle"));

        }, listOfAssets);
    }

    int GetBundleRootFolderChildCount()
    {
        Dictionary<string, BundleInfo>.ValueCollection childList = ABModelUtil.Root.GetChildList();
        return childList.Count;
    }

    void AddMaterialsToMultipleObjects(IEnumerable<string> parentNames, IEnumerable<string> paths, Material mat)
    {
        for(int i = 0; i < parentNames.Count(); i++)
        {
            GameObject p = GameObject.Find(parentNames.ElementAt(i));
            p.GetComponent<Renderer>().material = mat;

            PrefabUtility.ReplacePrefab(p, AssetDatabase.LoadMainAssetAtPath(paths.ElementAt(i)));
        }
    }

    string GetAllElementsAsString(IEnumerable<string> list)
    {
        string returnString = String.Empty;
        foreach (string i in list)
        {
            returnString += i + ", ";
        }

        return returnString;
    }
}
