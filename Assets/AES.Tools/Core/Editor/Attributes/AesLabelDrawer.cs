#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesLabelAttribute))]
    public class AesLabelDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            var attr = (AesLabelAttribute)attribute;
            EditorGUI.PropertyField(pos, prop, new GUIContent(attr.Text), true);
        }
    }
}
#endif