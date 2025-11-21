using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(AESButton))]
public class AESButtonEditor : ButtonEditor
{
    SerializedProperty throttleSeconds;
    SerializedProperty onPressed;
    SerializedProperty onDenied;
    SerializedProperty onAttentionPing;
    SerializedProperty onEnabledVisual;
    SerializedProperty onDisabledVisual;
    SerializedProperty onIdleStop;

    protected override void OnEnable()
    {
        base.OnEnable();

        throttleSeconds   = serializedObject.FindProperty("throttleSeconds");
        onPressed         = serializedObject.FindProperty("onPressed");
        onDenied          = serializedObject.FindProperty("onDenied");
        onAttentionPing   = serializedObject.FindProperty("onAttentionPing");
        onEnabledVisual   = serializedObject.FindProperty("onEnabledVisual");
        onDisabledVisual  = serializedObject.FindProperty("onDisabledVisual");
        onIdleStop        = serializedObject.FindProperty("onIdleStop");
    }

    public override void OnInspectorGUI()
    {
        // Button 기본 인스펙터
        base.OnInspectorGUI();

        // AESButton 전용 필드들
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("AES Button", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(throttleSeconds);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Unity Events - Feedback / Animations", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onPressed);
        EditorGUILayout.PropertyField(onDenied);
        EditorGUILayout.PropertyField(onAttentionPing);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Unity Events - State", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onEnabledVisual);
        EditorGUILayout.PropertyField(onDisabledVisual);
        EditorGUILayout.PropertyField(onIdleStop);

        serializedObject.ApplyModifiedProperties();
    }
}