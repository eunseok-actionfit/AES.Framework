// 파일: MonoContextEditor.cs (Editor 폴더)
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Editor
{
    [CustomEditor(typeof(MonoContext))]
    public class MonoContextEditor : UnityEditor.Editor
    {
        SerializedProperty _nameModeProp;
        SerializedProperty _customNameProp;
        SerializedProperty _viewModelSourceProp;
        SerializedProperty _viewModelTypeNameProp;

        void OnEnable()
        {
            _nameModeProp           = serializedObject.FindProperty("nameMode");
            _customNameProp         = serializedObject.FindProperty("customName");
            _viewModelSourceProp    = serializedObject.FindProperty("viewModelSource");
            _viewModelTypeNameProp  = serializedObject.FindProperty("viewModelTypeName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ------------------------------
            // 기본 Context 설정
            // ------------------------------
            EditorGUILayout.PropertyField(_nameModeProp);
            var nameMode = (ContextNameMode)_nameModeProp.enumValueIndex;
            if (nameMode == ContextNameMode.Custom)
                EditorGUILayout.PropertyField(_customNameProp);

            EditorGUILayout.PropertyField(_viewModelSourceProp);

            EditorGUILayout.Space(6);

            // ------------------------------
            // ViewModel Type 선택
            // ------------------------------
            DrawViewModelTypeField();

            EditorGUILayout.Space(10);

            // ------------------------------
            // 🔵 HelpBox: MenuHelp가 켜져 있을 때만 표시
            // ------------------------------
            if (MenuHelp.HelpEnabled)
            {
                EditorGUILayout.HelpBox(
                    "• ViewModel Type은 Path Binding 드롭다운(디자인타임)에서 사용하는 타입입니다.\n" +
                    "• 후보 검색 규칙: 클래스 이름이 반드시 'ViewModel'로 끝나야 합니다.\n" +
                    "• AutoCreate 모드일 경우 해당 타입으로 ViewModel 인스턴스를 생성합니다.\n" +
                    "• External 모드에서는 Presenter/Service에서 SetViewModel()로 수동 지정해야 합니다.",
                    MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawViewModelTypeField()
        {
            EditorGUILayout.LabelField("ViewModel Type", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            string savedName = _viewModelTypeNameProp.stringValue;
            Type currentType = null;

            if (!string.IsNullOrEmpty(savedName))
                currentType = Type.GetType(savedName);

            string label = currentType != null ? currentType.FullName : "(None)";
            EditorGUILayout.LabelField("Current", label);

            if (GUILayout.Button("Select ViewModel Type..."))
                ShowTypeMenu();

            EditorGUI.indentLevel--;
        }

        void ShowTypeMenu()
        {
            var menu = new GenericMenu();

            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a =>
                    !a.FullName.StartsWith("System") &&
                    !a.FullName.StartsWith("Unity"))
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
                })
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("ViewModel"))
                .OrderBy(t => t.FullName)
                .ToList();

            if (allTypes.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "ViewModel 타입 없음",
                    "'ViewModel'로 끝나는 타입을 찾지 못했습니다.",
                    "확인");
                return;
            }

            foreach (var t in allTypes)
            {
                string display = t.FullName;
                menu.AddItem(new GUIContent(display), false, () =>
                {
                    _viewModelTypeNameProp.stringValue = t.AssemblyQualifiedName;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }
    }
}
#endif
