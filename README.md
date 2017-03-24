# Unity Asset Bundle Browser tool

This tool can be dropped into any Unity project with a version of 5.6 or greater.

A new menu item is added in "Window" called "AssetBundle Browser":

Manage - This window gives an explorer like interface to managing and modifying asset bundles in your project.  
		 Changes are immediately applied to assets (Undo support is planned).  Selecting a bundle will show 
		 all explicitely included assets as well as an estimate of which assets will be pulled in due to 
		 dependencies.  These are greyed out to indicate that they are not explicit.  There are some cases where
		 dependencies are not pulled in correctly such as textures from materials.  
	   - This window will also attempt to analyze your bundles for warnings or errors. Data can get out of sync
	     with the project. The refresh button will force a sync and a re-analyze. The tool will look for:
			- duplicated assets (if an asset is implicitely included in more than 1 bundle)
			- Invalid scene bundles.  Scene bundles can only have a single scene asset.
			- (coming soon) Variant bundles mismatch issues.
	   - Right click on bundles or assets for content controls.

Build - Simple dialog to build bundles. 
