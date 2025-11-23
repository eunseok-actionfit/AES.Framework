using UnityEditor;
using UnityEngine;

namespace AES.Tools.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (ShowIfAttribute)attribute;
            var conditionProp = property.serializedObject.FindProperty(attr.ConditionFieldName);

            bool show = CheckCondition(conditionProp, attr);

            if (show)
                EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var attr = (ShowIfAttribute)attribute;
            var conditionProp = property.serializedObject.FindProperty(attr.ConditionFieldName);

            bool show = CheckCondition(conditionProp, attr);
            return show ? EditorGUI.GetPropertyHeight(property, label, true) : 0f;
        }

        private bool CheckCondition(SerializedProperty conditionProp, ShowIfAttribute attr)
        {
            // 조건 필드를 못 찾으면 그냥 보여주도록
            if (conditionProp == null)
                return true;

            switch (attr.Condition)
            {
                case ShowIfCondition.BoolIsTrue:
                    return conditionProp.propertyType == SerializedPropertyType.Boolean &&
                           conditionProp.boolValue;

                case ShowIfCondition.BoolIsFalse:
                    return conditionProp.propertyType == SerializedPropertyType.Boolean &&
                           !conditionProp.boolValue;

                case ShowIfCondition.Equals:
                    return CompareEquals(conditionProp, attr.RawValues);

                case ShowIfCondition.NotEquals:
                    return !CompareEquals(conditionProp, attr.RawValues);

                case ShowIfCondition.In:
                    return CompareIn(conditionProp, attr.RawValues);

                case ShowIfCondition.NotIn:
                    return !CompareIn(conditionProp, attr.RawValues);

                default:
                    return true;
            }
        }

        private bool CompareEquals(SerializedProperty conditionProp, object[] values)
        {
            if (values == null || values.Length == 0)
                return true;

            // Equals 는 첫 번째 값만 사용
            return CompareSingle(conditionProp, values[0]);
        }

        private bool CompareIn(SerializedProperty conditionProp, object[] values)
        {
            if (values == null || values.Length == 0)
                return true;

            foreach (var v in values)
            {
                if (CompareSingle(conditionProp, v))
                    return true;
            }
            return false;
        }

        private bool CompareSingle(SerializedProperty conditionProp, object value)
        {
            if (value == null)
                return false;

            switch (conditionProp.propertyType)
            {
                case SerializedPropertyType.Enum:
                    int enumInt = System.Convert.ToInt32(value);
                    return conditionProp.enumValueIndex == enumInt;

                case SerializedPropertyType.Integer:
                    int intVal = System.Convert.ToInt32(value);
                    return conditionProp.intValue == intVal;

                case SerializedPropertyType.Boolean:
                    bool boolVal = System.Convert.ToBoolean(value);
                    return conditionProp.boolValue == boolVal;

                case SerializedPropertyType.Float:
                    float floatVal = System.Convert.ToSingle(value);
                    return Mathf.Approximately(conditionProp.floatValue, floatVal);

                case SerializedPropertyType.String:
                    return string.Equals(conditionProp.stringValue, value.ToString(),
                        System.StringComparison.Ordinal);

                default:
                    return false;
            }
        }
    }
}
