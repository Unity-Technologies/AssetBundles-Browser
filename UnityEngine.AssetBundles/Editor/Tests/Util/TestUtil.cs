using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Boo.Lang.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.Tests.Util
{
    class TestUtil
    {
        /// <summary>
        /// Use this when you need to execute test code after creating assets.
        /// </summary>
        /// <param name="testCodeBlock">The test code</param>
        /// <param name="listOfPrefabs">List of paths to assets created for the test</param>
        public static void ExecuteCodeAndCleanupAssets(RuntimeServices.CodeBlock testCodeBlock, List<string> listOfPrefabs)
        {
            try
            {
                testCodeBlock();
            }
            catch (AssertionException ex)
            {
                Assert.Fail("Asserts threw an Assertion Exception.  The test failed." + ex.Message);
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception thrown when executing test" + ex.Message);
            }
            finally
            { 
                DestroyPrefabsAndRemoveUnusedBundleNames(listOfPrefabs);
            }
        }

        public static string CreatePrefabWithBundleAndVariantName(string bundleName, string variantName, string name = "Cube")
        {
            string path = "Assets/" + UnityEngine.Random.Range(0, 10000) + ".prefab";
            GameObject go = PrefabUtility.CreatePrefab(path, GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.name = name;
            AssetImporter.GetAtPath(path).SetAssetBundleNameAndVariant(bundleName, variantName);

            return path;
        }

        static void DestroyPrefabsAndRemoveUnusedBundleNames(IEnumerable<string> prefabPaths)
        {
            foreach (string prefab in prefabPaths)
            {
                AssetDatabase.DeleteAsset(prefab);
            }

            AssetDatabase.RemoveUnusedAssetBundleNames();
        }
    }
}
