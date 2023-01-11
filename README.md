# Unity Asset Bundle Browser tool

Note: This tool is not a supported utility.

This tool enables the user to view the content of built AssetBundles (on the "Inspect" tab).  This can be useful, but viewing extremely large AssetBundles can lead to slow performance and memory issues.

The "Configure" tab offers basic functionality for assigning Assets and Scenes to bundles, similar to using the AssetBundle control at the bottom of the Inspector.  

The "Build" tab offers basic functionality to assist in setting up a call to [BuildPipeline.BuildAssetBundles()](https://docs.unity3d.com/ScriptReference/BuildPipeline.BuildAssetBundles.html).

## Alternatives

It is recommended to use the [Addressables package](https://docs.unity3d.com/Packages/com.unity.addressables@latest) to define and build AssetBundles, rather than the Asset Bundle Browser.

[UnityDataTools](https://github.com/Unity-Technologies/UnityDataTools) is an alternative way to view the content of built AssetBundles.

## Installation
To install the Asset Bundle Browser:

* Open the Unity Package Manager in your Project (menu: Windows > Package Manager).
* Click the + (Add) button at the top, left corner of the window.
* Choose Add package from git URL…
* Enter https://github.com/Unity-Technologies/AssetBundles-Browser.git as the URL
* Click Add.
The Package Manager downloads and installs the package’s “master” branch.

Once installed it will create a new menu item in *Window->AssetBundle Browser*.  

## Full Documentation

See the included [project manual page](Documentation/com.unity.assetbundlebrowser.md).

