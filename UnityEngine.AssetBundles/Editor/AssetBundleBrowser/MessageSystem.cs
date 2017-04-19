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
            private MessageFlag m_messageFlags;
            private HashSet<MessageFlag> m_messageSet; 
            public MessageState()
            {
                m_messageFlags = MessageFlag.None;
                m_messageSet = new HashSet<MessageFlag>();
            }

            public void Clear()
            {
                m_messageFlags = MessageFlag.None;
                m_messageSet.Clear();
            }

            public void SetFlag(MessageFlag flag, bool on)
            {
                if (flag == MessageFlag.Info || flag == MessageFlag.Warning || flag == MessageFlag.Error)
                    return;

                if (on)
                {
                    m_messageFlags |= flag;
                    m_messageSet.Add(flag);
                }
                else
                {
                    m_messageFlags &= ~flag;
                    m_messageSet.Remove(flag);
                }
            }
            public bool IsSet(MessageFlag flag)
            {
                return (m_messageFlags & flag) == flag;
            }
            public bool HasMessages()
            {
                return (m_messageFlags != MessageFlag.None);
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
                foreach(var f in m_messageSet)
                {
                    if (f > high)
                        high = f;
                }
                return high;
            }

            public List<Message> GetMessages()
            {
                var msgs = new List<Message>();
                foreach(var f in m_messageSet)
                {
                    msgs.Add(GetMessage(f));
                }
                return msgs;
            }
        }
        public static Texture2D GetIcon(MessageType sev)
        {
            if (sev == MessageType.Error)
                return EditorGUIUtility.FindTexture("console.errorIcon");
            else if (sev == MessageType.Warning)
                return EditorGUIUtility.FindTexture("console.warnicon");
            else if (sev == MessageType.Info)
                return EditorGUIUtility.FindTexture("console.infoIcon");
            else
                return null;
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


        private static Dictionary<MessageFlag, Message> m_messageLookup = null;
        public static Message GetMessage(MessageFlag lookup)
        {
            if (m_messageLookup == null)
                InitMessages();

            Message msg = null;
            m_messageLookup.TryGetValue(lookup, out msg);
            if (msg == null)
                msg = m_messageLookup[MessageFlag.None];
            return msg;
        }

        private static void InitMessages()
        {
            m_messageLookup = new Dictionary<MessageFlag, Message>();

            m_messageLookup.Add(MessageFlag.None, new Message(string.Empty, MessageType.None));
            m_messageLookup.Add(MessageFlag.EmptyBundle, new Message("This bundle is empty.  Empty bundles cannot get saved with the scene and will disappear from this list if Unity restarts or if various other bundle rename or move events occur.", MessageType.Info));
            m_messageLookup.Add(MessageFlag.EmptyFolder, new Message("This folder is either empty or contains only empty children.  Empty bundles cannot get saved with the scene and will disappear from this list if Unity restarts or if various other bundle rename or move events occur.", MessageType.Info));
            m_messageLookup.Add(MessageFlag.WarningInChildren, new Message("Warning in child(ren)", MessageType.Warning));
            m_messageLookup.Add(MessageFlag.AssetsDuplicatedInMultBundles, new Message("Assets being pulled into this bundle due to dependencies are also being pulled into another bundle.  This will cause duplication in memory", MessageType.Warning));
            m_messageLookup.Add(MessageFlag.VariantBundleMismatch, new Message("hi", MessageType.Warning));
            m_messageLookup.Add(MessageFlag.ErrorInChildren, new Message("Error in child(ren)", MessageType.Error));
            m_messageLookup.Add(MessageFlag.SceneBundleConflict, new Message("A bundle with a scene must only contain that one scene.  This bundle has more than one explicitly added bundles.", MessageType.Error));
            m_messageLookup.Add(MessageFlag.DependencySceneConflict, new Message("The folder added to this bundle has pulled in more than one scene which is not allowed.", MessageType.Error));
        }
    }

}