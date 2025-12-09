#if UNITY_EDITOR && !ODIN_INSPECTOR
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(AesShowIfAttribute))]
    public class AesShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (AesShowIfAttribute)attribute;
            var target = property.serializedObject.targetObject;

            if (!AesConditionEvaluator.EvaluateRaw(target, attr))
                return;

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (AesShowIfAttribute)attribute;
            var target = property.serializedObject.targetObject;

            return AesConditionEvaluator.EvaluateRaw(target, attr)
                ? EditorGUI.GetPropertyHeight(property, label, true)
                : 0f;
        }
    }

    [CustomPropertyDrawer(typeof(AesHideIfAttribute))]
    public class AesHideIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (AesHideIfAttribute)attribute;
            var target = property.serializedObject.targetObject;

            if (AesConditionEvaluator.EvaluateRaw(target, attr))
                return;

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (AesHideIfAttribute)attribute;
            var target = property.serializedObject.targetObject;

            return AesConditionEvaluator.EvaluateRaw(target, attr)
                ? 0f
                : EditorGUI.GetPropertyHeight(property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(AesEnableIfAttribute))]
    public class AesEnableIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (AesEnableIfAttribute)attribute;
            var target = property.serializedObject.targetObject;

            bool cond = AesConditionEvaluator.EvaluateRaw(target, attr);

            bool prev = GUI.enabled;
            GUI.enabled = prev && cond;

            EditorGUI.PropertyField(position, property, label, true);

            GUI.enabled = prev;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }

    [CustomPropertyDrawer(typeof(AesDisableIfAttribute))]
    public class AesDisableIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (AesDisableIfAttribute)attribute;
            var target = property.serializedObject.targetObject;

            bool cond = AesConditionEvaluator.EvaluateRaw(target, attr);

            bool prev = GUI.enabled;
            GUI.enabled = prev && !cond;

            EditorGUI.PropertyField(position, property, label, true);

            GUI.enabled = prev;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}
#endif
