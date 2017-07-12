# Unity Asset Bundle Browser tool

This tool enables the user to view and edit the configuration of asset bundles for their Unity project.  It will block editing that would create invalid bundles, and inform you of any issues with existing bundles.  It also provides basic build functionality.

This tool is intended to replace the current workflow of selecting assets and setting their asset bundle manually in the inspector.  It can be dropped into any Unity project with a version of 5.6 or greater.  It will create a new menu item in *Window->AssetBundle Browser*.  

## FUll Documentation
#### Official Released Features
See [the official manual page](https://docs.unity3d.com/Manual/AssetBundles-Browser.html) or view the included [project manual page](MANUAL.md)
#### Beta Feature - Inspect Tab
This is a new tab added as a currently-beta feature.  Use it to inspect the contents of bundles that have already been built. 
##### Usage
* Type in a bundle path, or find one using the 'Browse' button.  
* Select any bundle listed to see details:
  * Name
  * Size on disk
  * Preload Table - full contents of bundle
  * Container - only explicitly added items
  * Dependencies - bundles that the current bundle depends on

##### Issues
* If your "bundle path" contains duplicate bundles, you will be unable to view them, and will get errors in the log.  This is most often seen if you had a file structure like "MyBundles/Build1/" and "MyBundles/Build2/" and you pointed the path to just "MyBundles"
* The tool does not yet handle bundles that have had the hash appended to the bundle name (using "append hash" build option).