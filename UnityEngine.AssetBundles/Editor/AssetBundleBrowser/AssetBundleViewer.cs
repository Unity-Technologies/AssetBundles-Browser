using UnityEngine;
using UnityEditor;

public class AssetBundleViewer : EditorWindow
{
    AssetBundle assetBundle;
    Object obj;
    Editor editor;
    WWW www;
    string error;
    string path = null;
    string uiPath = null;
    string progress = null;
    Vector3 scrollPos;
    /*
    [MenuItem("AssetBundles/Viewer")]
    public static void OpenViewer()
    {
        GetWindow(typeof(AssetBundleViewer));
    }*/

    void Update()
    {
        if (www != null)
        {
            if (www.error != null)
            {
                error = www.error;
                SafeDisposeWWW();
            }
            else
            {
                error = null;
                if (www.isDone)
                {
                    AssetBundle ab = www.assetBundle;
                    if (ab != null)
                    {
                        SafeUnloadAssetBundle();
                        assetBundle = ab;
                    }
                    SafeDisposeWWW();
                    editor = Editor.CreateEditor(assetBundle);
                    progress = null;
                }
                else
                {
                    progress = www.progress * 100 + "%";
                }
            }
            Repaint();
        }
    }

    void OnGUI()
    {
        obj = EditorGUILayout.ObjectField("AssetBundle", obj, typeof(Object), false);
        if (GUI.changed && obj != null)
        {
            LoadAB(null);
        }

        EditorGUILayout.BeginHorizontal();
        uiPath = EditorGUILayout.TextField("Path", uiPath);
        GUI.enabled = uiPath != null;
        if (GUILayout.Button("Open"))
        {
            LoadAB(uiPath);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (error != null)
            EditorGUILayout.LabelField("Error: " + error);
        if (progress != null)
            EditorGUILayout.LabelField("Progress: " + progress);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        if (editor != null)
        {
            editor.Repaint();
            editor.OnInspectorGUI();
        }
        EditorGUILayout.EndScrollView();

        if (assetBundle != null)
        {
            if (GUILayout.Button("Unload Asset Bundle"))
            {
                SafeUnloadAssetBundle();
            }
            if (GUILayout.Button("Instantiate Main Asset"))
            {
                Editor.Instantiate(assetBundle.mainAsset);
            }
        }
    }

    void OnDestroy()
    {
        SafeDisposeWWW();
        SafeUnloadAssetBundle();
    }

    void SafeDisposeWWW()
    {
        if (www != null)
        {
            www.Dispose();
            www = null;
        }
    }

    void SafeUnloadAssetBundle()
    {
        if (assetBundle != null)
        {
            assetBundle.Unload(true);
            assetBundle = null;
            editor = null;
        }
    }

    void LoadAB(string pathToBundle)
    {
        SafeUnloadAssetBundle();
        SafeDisposeWWW();

        if (pathToBundle != null)
        {
            www = new WWW(uiPath);
        }
        else
            if (obj != null)
        {
            path = AssetDatabase.GetAssetPath(obj);
            path = "file://" + System.IO.Directory.GetParent(Application.dataPath) + "/" + path;
            www = new WWW(path);
        }
    }
}