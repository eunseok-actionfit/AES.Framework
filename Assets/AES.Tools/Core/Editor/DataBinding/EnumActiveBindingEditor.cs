#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


// ContextBindingBaseEditor 네임스페이스 맞춰서

namespace AES.Tools.Editor
{
    [CustomEditor(typeof(EnumActiveBinding))]
    public class EnumActiveBindingEditor : ContextBindingBaseEditor
    {
        SerializedProperty _targetProp;
        SerializedProperty _enumNameProp;
        SerializedProperty _invertProp;
        SerializedProperty _enumNamesProp;
        SerializedProperty _enumIndexProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            _targetProp    = serializedObject.FindProperty("target");
            _enumNameProp  = serializedObject.FindProperty("enumName");
            _invertProp    = serializedObject.FindProperty("invert");
            _enumNamesProp = serializedObject.FindProperty("_enumNames");
            _enumIndexProp = serializedObject.FindProperty("_enumIndex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1) 기본 바인딩 섹션 (Context / MemberPath 등)
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Enum Active", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_targetProp);

            // 2) Enum 드롭다운
            var names = GetEnumNamesFromProperty(_enumNamesProp);
            if (names != null && names.Length > 0)
            {
                int index = Mathf.Clamp(_enumIndexProp.intValue, 0, names.Length - 1);

                EditorGUI.BeginChangeCheck();
                index = EditorGUILayout.Popup("Enum", index, names);
                if (EditorGUI.EndChangeCheck())
                {
                    _enumIndexProp.intValue = index;
                    _enumNameProp.stringValue = names[index];
                }
            }
            else
            {
                // 아직 enum 타입을 파악 못한 경우 (에디터에서 값 못 읽은 상태 등)
                EditorGUILayout.HelpBox(
                    "플레이 중에 Enum 값이 한 번 들어오면 드롭다운이 채워집니다.\n" +
                    "지금은 직접 Enum 이름을 입력하세요.",
                    MessageType.Info);
                EditorGUILayout.PropertyField(_enumNameProp, new GUIContent("Enum Name"));
            }

            EditorGUILayout.PropertyField(_invertProp);

            serializedObject.ApplyModifiedProperties();
        }

        string[] GetEnumNamesFromProperty(SerializedProperty arrayProp)
        {
            if (arrayProp == null || !arrayProp.isArray || arrayProp.arraySize == 0)
                return null;

            var list = new string[arrayProp.arraySize];
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                list[i] = arrayProp.GetArrayElementAtIndex(i).stringValue;
            }
            return list;
        }
    }
}
#endif
