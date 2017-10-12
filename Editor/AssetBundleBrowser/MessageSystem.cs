using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace UnityEngine.AssetBundles
{
    public class MessageSystem
    {
        private static Texture2D m_ErrorIcon = null;
        private static Texture2D m_WarningIcon = null;
        private static Texture2D m_InfoIcon = null;
        private static Dictionary<MessageFlag, Message> m_MessageLookup = null;

        [Flags]
        public enum MessageFlag
        {
            None = 0x0,

            Info = 0x80,                  //this flag is only used to check bits, not set.
            EmptyBundle = 0x81,
            EmptyFolder = 0x82,

            Warning = 0x8000,                  //this flag is only used to check bits, not set.
            WarningInChildren = 0x8100,
            AssetsDuplicatedInMultBundles = 0x8200,
            VariantBundleMismatch = 0x8400,

            Error = 0x800000,                  //this flag is only used to check bits, not set.
            ErrorInChildren = 0x810000,
            SceneBundleConflict = 0x820000,
            DependencySceneConflict = 0x840000,
        }

        public class MessageState
        {
            //I have an enum and a set of enums to make some logic cleaner.  
            // The enum has masks for Error/Warning/Info that won't ever be in the set
            // this allows for easy checking of IsSet for error rather than specific errors. 
            private MessageFlag m_MessageFlags;
            private HashSet<MessageFlag> m_MessageSet;


            public MessageState()
            {
                m_MessageFlags = MessageFlag.None;
                m_MessageSet = new HashSet<MessageFlag>();
            }

            public void Clear()
            {
                m_MessageFlags = MessageFlag.None;
                m_MessageSet.Clear();
            }

            public void SetFlag(MessageFlag flag, bool on)
            {
                if (flag == MessageFlag.Info || flag == MessageFlag.Warning || flag == MessageFlag.Error)
                    return;

                if (on)
                {
                    m_MessageFlags |= flag;
                    m_MessageSet.Add(flag);
                }
                else
                {
                    m_MessageFlags &= ~flag;
                    m_MessageSet.Remove(flag);
                }
            }
            public bool IsSet(MessageFlag flag)
            {
                return (m_MessageFlags & flag) == flag;
            }
            public bool HasMessages()
            {
                return (m_MessageFlags != MessageFlag.None);
            }

            public MessageType HighestMessageLevel()
            {
                if (IsSet(MessageFlag.Error))
                    return MessageType.Error;
                if (IsSet(MessageFlag.Warning))
                    return MessageType.Warning;
                if (IsSet(MessageFlag.Info))
                    return MessageType.Info;
                return MessageType.None;
            }
            public MessageFlag HighestMessageFlag()
            {
                MessageFlag high = MessageFlag.None;
                foreach(var f in m_MessageSet)
                {
                    if (f > high)
                        high = f;
                }
                return high;
            }

            public List<Message> GetMessages()
            {
                var msgs = new List<Message>();
                foreach(var f in m_MessageSet)
                {
                    msgs.Add(GetMessage(f));
                }
                return msgs;
            }
        }
        public static Texture2D GetIcon(MessageType sev)
        {
            if (sev == MessageType.Error)
                return GetErrorIcon();
            else if (sev == MessageType.Warning)
                return GetWarningIcon();
            else if (sev == MessageType.Info)
                return GetInfoIcon();
            else
                return null;
        }
        private static Texture2D GetErrorIcon()
        {
            if (m_ErrorIcon == null)
                FindMessageIcons();
            return m_ErrorIcon;
        }
        private static Texture2D GetWarningIcon()
        {
            if (m_WarningIcon == null)
                FindMessageIcons();
            return m_WarningIcon;
        }
        private static Texture2D GetInfoIcon()
        {
            if (m_InfoIcon == null)
                FindMessageIcons();
            return m_InfoIcon;
        }

        private static void FindMessageIcons()
        {
            m_ErrorIcon = EditorGUIUtility.FindTexture("console.errorIcon");
            m_WarningIcon = EditorGUIUtility.FindTexture("console.warnicon");
            m_InfoIcon = EditorGUIUtility.FindTexture("console.infoIcon");
        }
        public class Message
        {
            public Message(string msg, MessageType sev)
            {
                message = msg;
                severity = sev;
            }

            public MessageType severity;
            public string message;
            public Texture2D icon
            {
                get
                {
                    return GetIcon(severity);
                }
            }
        }

        public static Message GetMessage(MessageFlag lookup)
        {
            if (m_MessageLookup == null)
                InitMessages();

            Message msg = null;
            m_MessageLookup.TryGetValue(lookup, out msg);
            if (msg == null)
                msg = m_MessageLookup[MessageFlag.None];
            return msg;
        }

        private static void InitMessages()
        {
            m_MessageLookup = new Dictionary<MessageFlag, Message>();

            m_MessageLookup.Add(MessageFlag.None, new Message(string.Empty, MessageType.None));
            m_MessageLookup.Add(MessageFlag.EmptyBundle, new Message("This bundle is empty.  Empty bundles cannot get saved with the scene and will disappear from this list if Unity restarts or if various other bundle rename or move events occur.", MessageType.Info));
            m_MessageLookup.Add(MessageFlag.EmptyFolder, new Message("This folder is either empty or contains only empty children.  Empty bundles cannot get saved with the scene and will disappear from this list if Unity restarts or if various other bundle rename or move events occur.", MessageType.Info));
            m_MessageLookup.Add(MessageFlag.WarningInChildren, new Message("Warning in child(ren)", MessageType.Warning));
            m_MessageLookup.Add(MessageFlag.AssetsDuplicatedInMultBundles, new Message("Assets being pulled into this bundle due to dependencies are also being pulled into another bundle.  This will cause duplication in memory", MessageType.Warning));
            m_MessageLookup.Add(MessageFlag.VariantBundleMismatch, new Message("Variants of a given bundle must have exactly the same assets between them based on a Name.Extension (without Path) comparison. These bundle variants fail that check.", MessageType.Warning));
            m_MessageLookup.Add(MessageFlag.ErrorInChildren, new Message("Error in child(ren)", MessageType.Error));
            m_MessageLookup.Add(MessageFlag.SceneBundleConflict, new Message("A bundle with one or more scenes must only contain scenes.  This bundle has scenes and non-scene assets.", MessageType.Error));
            m_MessageLookup.Add(MessageFlag.DependencySceneConflict, new Message("The folder added to this bundle has pulled in scenes and non-scene assets.  A bundle must only have one type or the other.", MessageType.Error));
        }
    }

}