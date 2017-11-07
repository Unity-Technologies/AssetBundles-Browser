using UnityEditor;
using UnityEngine;
using System.IO;

namespace AssetBundleBrowser
{
    class SingleBundleInspector
    {
        internal static string currentPath { get; set; }


        internal SingleBundleInspector() { }

        private Editor m_Editor = null;

        private Rect m_Position;

        [SerializeField]
        private Vector2 m_ScrollPosition;

        internal void SetBundle(AssetBundle bundle, string path = "")
        {
            //static var...
            currentPath = path;

            //members
            m_Editor = null;
            if (bundle != null)
                m_Editor = Editor.CreateEditor(bundle);
        }

        internal void OnGUI(Rect pos)
        {
            if (m_Editor == null)
                return;

            m_Position = pos;

            DrawBundleData();
        }

        private void DrawBundleData()
        {
            GUILayout.BeginArea(m_Position);
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            m_Editor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }

    [CustomEditor(typeof(AssetBundle))]
    internal class LevelScriptEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            AssetBundle bundle = target as AssetBundle;

            using (new EditorGUI.DisabledScope(true))
            {
                var leftStyle = GUI.skin.GetStyle("Label");
                leftStyle.alignment = TextAnchor.UpperLeft;
                GUILayout.Label(new GUIContent("Name: " + bundle.name), leftStyle);

                long fileSize = -1;
                if(SingleBundleInspector.currentPath != string.Empty && File.Exists(SingleBundleInspector.currentPath) )
                {
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(SingleBundleInspector.currentPath);
                    fileSize = fileInfo.Length;
                }

                if (fileSize < 0)
                    GUILayout.Label(new GUIContent("Size: unknown"), leftStyle);
                else
                    GUILayout.Label(new GUIContent("Size: " + EditorUtility.FormatBytes(fileSize)), leftStyle);

            }

            base.OnInspectorGUI();
        }
    }
}