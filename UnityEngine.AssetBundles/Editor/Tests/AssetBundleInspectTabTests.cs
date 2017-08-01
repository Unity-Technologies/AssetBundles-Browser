using System;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEditor;
using UnityEngine;
using UnityEngine.AssetBundles;

public class AssetBundleInspectTabTests
{
    [Test]
    public void GetPathWithoutFileExtension()
    {
        Assert.IsNull(this.GetPathUtil(null));
        Assert.AreEqual(string.Empty, this.GetPathUtil(string.Empty));
        Assert.AreEqual("/dir/dir2/file", this.GetPathUtil("/dir/dir2/file.ext"));
        Assert.AreEqual("/dir/dir2/file.ext", this.GetPathUtil("/dir/dir2/file.ext.ext"));
        Assert.AreEqual("/dir/dir2/file", this.GetPathUtil("/dir/dir2/file"));
        Assert.AreEqual("/dir/dir2", this.GetPathUtil("/dir/dir2/"));
        Assert.AreEqual("file", this.GetPathUtil("file.ext"));
        Assert.AreEqual("file", this.GetPathUtil("file"));
        Assert.AreEqual(string.Empty, this.GetPathUtil("/"));
    }

    private string GetPathUtil(string input)
    {
        return AssetBundleInspectTab.GetPathWithoutFileExtension(input);
    }
}