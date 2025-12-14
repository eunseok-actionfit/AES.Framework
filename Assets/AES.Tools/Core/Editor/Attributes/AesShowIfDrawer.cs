#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesShowIfAttribute))]
    [CustomPropertyDrawer(typeof(AesEnableIfAttribute))]
    public class AesConditionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            var attr = (AesConditionAttribute)attribute;
            var target = prop.serializedObject.targetObject;

            bool cond = AesConditionUtil.Evaluate(target, attr.Member);
            if (attr.Invert) cond = !cond;

            if (attribute is AesShowIfAttribute)
            {
                if (!cond) return;
                EditorGUI.PropertyField(pos, prop, label, true);
            }
            else
            {
                bool prev = GUI.enabled;
                GUI.enabled = prev && cond;
                EditorGUI.PropertyField(pos, prop, label, true);
                GUI.enabled = prev;
            }
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            var attr = (AesConditionAttribute)attribute;
            var target = prop.serializedObject.targetObject;

            bool cond = AesConditionUtil.Evaluate(target, attr.Member);
            if (attr.Invert) cond = !cond;

            if (attribute is AesShowIfAttribute && !cond)
                return 0f;

            return EditorGUI.GetPropertyHeight(prop, label, true);
        }
    }
}
#endif