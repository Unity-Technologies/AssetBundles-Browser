using UnityEngine;
using UnityEditor;
using System.IO;

namespace UnityEngine.AssetBundles.Old
{
	public class AssetBundleBuildWindow : EditorWindow
	{

		void OnGUI()
		{
			if (GUILayout.Button("Build"))
			{
				string path = EditorUtility.SaveFolderPanel("Select Folder for AssetBundles", "", "");
				if (path != string.Empty)
					BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
			}

		}
	}
}
