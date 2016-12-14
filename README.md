# Unity Asset Bundle Browser tool

This collection of tools can be dropped into any Unity project with a version of 5.6 or greater.

A new top level menu named "AssetBundles" will be added to give access to these tools:

Manage - This window gives an explorer like interface to managing and modifying asset bundles in your project.  
		 Changes are immediately applied to assets (Undo support is planned).  Selecting a bundle will show 
		 all explicitely included assets as well as an estimate of which assets will be pulled in due to 
		 dependencies.  These are greyed out to indicate that they are not explicit.  There are some cases where
		 dependencies are not pulled in correctly such as textures from materials.  

Analyze - This utility will detect some common errors with AssetBundles.  The currently detected issues are:
	- duplicated assets (if an asset is implicitely included in more than 1 bundle)
	- Invalid scene bundles.  Scene bundles can only have a single scene asset.
	- Mismatched variant bundles.  Asset names of variant bundles must match fully

Build - Simple dialog to build bundles. 

Inspect - This utility will load built bundles from a folder and show their contents.

Reset - Resets ALL AssetBundle information in the project.  Use with caution.