using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using System.Linq;
using System;


namespace UnityEngine.AssetBundles
{
    internal class MessageList
    {
        private Vector2 m_scrollPosition = Vector2.zero;

        private GUIStyle[] m_style = new GUIStyle[2];

        IEnumerable<AssetBundleModel.AssetInfo> m_selecteditems;
        List<MessageSystem.Message> m_messages;

        Vector2 m_dimensions = new Vector2(0, 0);
        const float kScrollbarPadding = 16f;
        const float kBorderSize = 1f;


        public MessageList()
        {
            Init();
        }
        private void Init()
        {
            m_style[0] = "OL EntryBackOdd";
            m_style[1] = "OL EntryBackEven";
            m_style[0].wordWrap = true;
            m_style[1].wordWrap = true;
            m_style[0].padding = new RectOffset(32, 0, 1, 4);
            m_style[1].padding = new RectOffset(32, 0, 1, 4);
            m_messages = new List<MessageSystem.Message>();

        }
        public void OnGUI(Rect fullPos)
        {
            DrawOutline(fullPos, 1f);

            Rect pos = new Rect(fullPos.x + kBorderSize, fullPos.y + kBorderSize, fullPos.width - 2 * kBorderSize, fullPos.height - 2 * kBorderSize);
            

            if (m_dimensions.y == 0 || m_dimensions.x != pos.width - kScrollbarPadding)
            {
                //recalculate height.
                m_dimensions.x = pos.width - kScrollbarPadding;
                m_dimensions.y = 0;
                foreach (var message in m_messages)
                {
                    m_dimensions.y += m_style[0].CalcHeight(new GUIContent(message.message), m_dimensions.x);
                }
            }

            m_scrollPosition = GUI.BeginScrollView(pos, m_scrollPosition, new Rect(0, 0, m_dimensions.x, m_dimensions.y));
            int counter = 0;
            float runningHeight = 0.0f;
            foreach (var message in m_messages) 
            {
                int index = counter % 2;
                var content = new GUIContent(message.message);
                float height = m_style[index].CalcHeight(content, m_dimensions.x);

                GUI.Box(new Rect(0, runningHeight, m_dimensions.x, height), content, m_style[index]);
                GUI.DrawTexture(new Rect(0, runningHeight, 32f, 32f), message.icon);
                //TODO - cleanup formatting issues and switch to HelpBox
                //EditorGUI.HelpBox(new Rect(0, runningHeight, m_dimensions.x, height), message.message, (MessageType)message.severity);

                counter++;
                runningHeight += height;
            }
            GUI.EndScrollView();
        }

        internal void SetItems(IEnumerable<AssetBundleModel.AssetInfo> items)
        {
            m_selecteditems = items;
            CollectMessages();
        }

        internal void CollectMessages()
        {
            m_messages.Clear();
            m_dimensions.y = 0f;
            if(m_selecteditems != null)
            {
                foreach (var asset in m_selecteditems)
                {
                    m_messages.AddRange(asset.GetMessages());
                }
            }
        }

        internal void DrawOutline(Rect rect, float size)
        {
            Color color = new Color(0.6f, 0.6f, 0.6f, 1.333f);
            if(EditorGUIUtility.isProSkin)
            {
                color.r = 0.12f;
                color.g = 0.12f;
                color.b = 0.12f;
            }

            if (Event.current.type != EventType.repaint)
                return;

            Color orgColor = GUI.color;
            GUI.color = GUI.color * color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - size, rect.width, size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - size, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);

            GUI.color = orgColor;
        }
    }
}
