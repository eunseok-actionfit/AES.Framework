#if UNITY_EDITOR && !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesInformationAttribute))]
    public class AesInformationDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (AesInformationAttribute)attribute;

            var helpBoxHeight = GetHelpBoxHeight(attr);
            var propHeight = EditorGUI.GetPropertyHeight(property, label, true);
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var propRect = new Rect(position.x, position.y, position.width, propHeight);
            var helpRect = new Rect(position.x, position.y, position.width, helpBoxHeight);

            if (string.IsNullOrEmpty(attr.Message))
            {
                EditorGUI.PropertyField(propRect, property, label, true);
                return;
            }

            if (attr.MessageAfterProperty)
            {
                // 1) Property
                EditorGUI.PropertyField(propRect, property, label, true);

                // 2) HelpBox
                helpRect.y = propRect.yMax + spacing;
                EditorGUI.HelpBox(helpRect, attr.Message, ToUnityMessageType(attr.Type));
            }
            else
            {
                // 1) HelpBox
                EditorGUI.HelpBox(helpRect, attr.Message, ToUnityMessageType(attr.Type));

                // 2) Property
                propRect.y = helpRect.yMax + spacing;
                EditorGUI.PropertyField(propRect, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (AesInformationAttribute)attribute;

            var helpBoxHeight = GetHelpBoxHeight(attr);
            var propHeight = EditorGUI.GetPropertyHeight(property, label, true);
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            if (string.IsNullOrEmpty(attr.Message))
                return propHeight;

            return helpBoxHeight + spacing + propHeight;
        }

        private float GetHelpBoxHeight(AesInformationAttribute attr)
        {
            if (string.IsNullOrEmpty(attr.Message))
                return 0f;

            // 대충 2줄 이상 나오면 좀 더 크게
            return Mathf.Max(
                EditorGUIUtility.singleLineHeight * 2f,
                EditorGUIUtility.singleLineHeight *
                (1f + attr.Message.Split('\n').Length * 0.6f)
            );
        }
        
        private MessageType ToUnityMessageType(AesInformationAttribute.InfoType type)
        {
            switch (type)
            {
                case AesInformationAttribute.InfoType.Info:
                    return MessageType.Info;
                case AesInformationAttribute.InfoType.Warning:
                    return MessageType.Warning;
                case AesInformationAttribute.InfoType.Error:
                    return MessageType.Error;
                default:
                    return MessageType.None;
            }
        }
    }
}
#endif