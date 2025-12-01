#if UNITY_EDITOR
using UnityEditor;
using AES.Tools.Editor;

[CustomEditor(typeof(ValueConditionBinding))]
public class ValueConditionBindingEditor : ContextBindingBaseEditor
{
    SerializedProperty _opProp;
    SerializedProperty _typeProp;
    SerializedProperty _intProp;
    SerializedProperty _floatProp;
    SerializedProperty _boolProp;
    SerializedProperty _stringProp;
    SerializedProperty _enumNameProp;
    SerializedProperty _onEvaluatedProp;
    SerializedProperty _onBecameTrueProp;
    SerializedProperty _onBecameFalseProp;

    protected override void OnEnable()
    {
        base.OnEnable();

        _opProp           = serializedObject.FindProperty("op");
        _typeProp         = serializedObject.FindProperty("valueType");
        _intProp          = serializedObject.FindProperty("intValue");
        _floatProp        = serializedObject.FindProperty("floatValue");
        _boolProp         = serializedObject.FindProperty("boolValue");
        _stringProp       = serializedObject.FindProperty("stringValue");
        _enumNameProp     = serializedObject.FindProperty("enumSelectedName");
        _onEvaluatedProp  = serializedObject.FindProperty("OnEvaluated");
        _onBecameTrueProp = serializedObject.FindProperty("OnBecameTrue");
        _onBecameFalseProp= serializedObject.FindProperty("OnBecameFalse");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Condition", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(_opProp);
        EditorGUILayout.PropertyField(_typeProp);

        var type = (ConditionValueType)_typeProp.enumValueIndex;

        switch (type)
        {
            case ConditionValueType.Int:
                EditorGUILayout.PropertyField(_intProp);
                break;
            case ConditionValueType.Float:
                EditorGUILayout.PropertyField(_floatProp);
                break;
            case ConditionValueType.Bool:
                EditorGUILayout.PropertyField(_boolProp);
                break;
            case ConditionValueType.String:
                EditorGUILayout.PropertyField(_stringProp);
                break;
            case ConditionValueType.Enum:
                EditorGUILayout.PropertyField(_enumNameProp);
                break;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_onEvaluatedProp);
        EditorGUILayout.PropertyField(_onBecameTrueProp);
        EditorGUILayout.PropertyField(_onBecameFalseProp);

        // 1) 부모 에디터(컨텍스트 + 멤버 경로) 그리기
        base.OnInspectorGUI();
        
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
