#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AES.Tools;
using AES.Tools.Commands;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DataBinding))]
public class DataBindingDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var modeProp   = property.FindPropertyRelative("mode");
        var pathProp   = property.FindPropertyRelative("path");
        var constProp  = property.FindPropertyRelative("constant");
        var refProp    = property.FindPropertyRelative("reference");
        var provProp   = property.FindPropertyRelative("provider");
        var dtTypeProp = property.FindPropertyRelative("designTimeViewModelTypeName");

        var line = position;
        line.height = EditorGUIUtility.singleLineHeight;

        // 1) Mode
        EditorGUI.PropertyField(line, modeProp);
        line.y += line.height + 2;

        var mode = (BindingMode)modeProp.enumValueIndex;

        // 2) 모드별 필드
        switch (mode)
        {
            case BindingMode.Constant:
                EditorGUI.PropertyField(line, constProp, new GUIContent("Constant"));
                break;

            case BindingMode.Reference:
                EditorGUI.PropertyField(line, refProp, new GUIContent("Reference"));
                break;

            case BindingMode.Provider:
                EditorGUI.PropertyField(line, provProp, new GUIContent("Provider"));
                break;

            case BindingMode.Path:
                DrawPathField(line, property, pathProp, dtTypeProp);
                break;
        }

        EditorGUI.EndProperty();
    }

    void DrawPathField(Rect line, SerializedProperty bindingProp,
        SerializedProperty pathProp, SerializedProperty dtTypeProp)
    {
        var pathRect = line;
        pathRect.width -= 80;

        var btnRect = line;
        btnRect.x += pathRect.width + 4;
        btnRect.width = 76;

        // 경로 텍스트
        EditorGUI.TextField(pathRect, "Path", pathProp.stringValue);

        // Select 버튼
        if (GUI.Button(btnRect, "Select..."))
        {
            ShowPathMenu(bindingProp, pathProp, dtTypeProp);
        }
    }

    void ShowPathMenu(SerializedProperty bindingProp,
        SerializedProperty pathProp, SerializedProperty dtTypeProp)
    {
        // 1) 우선 현재 ContextBindingBase에서 Context를 얻을 수 있는지 시도
        var targetObj = bindingProp.serializedObject.targetObject as ContextBindingBase;
        Type vmType = null;
        object vmInstance = null;

        var ctx = targetObj != null ? GetContextForEditor(targetObj) : null;
        if (ctx != null)
        {
            vmType     = ctx.GetViewModelType();
            vmInstance = ctx.GetDesignTimeViewModel();
        }

        // 2) 그래도 못 찾으면 DataBinding.designTimeViewModelTypeName 사용
        if (vmType == null)
        {
            var typeName = dtTypeProp.stringValue;
            if (!string.IsNullOrEmpty(typeName))
            {
                vmType = Type.GetType(typeName);
            }
        }

        // 3) 아직도 없으면 타입 없음
        if (vmType == null)
        {
            EditorUtility.DisplayDialog(
                "ViewModel 타입 없음",
                "씬에서 Context를 찾지 못했고, Design-time ViewModel Type도 설정되어 있지 않습니다.\n" +
                "먼저 Design-time ViewModel 타입을 지정해 주세요.(designTimeViewModelTypeName)",
                "확인");
            return;
        }

        // 후보 만들기 (기존 BuildCandidates 로직 재사용)
        var candidates = new List<(string path, string label)>();
        BuildCandidates(vmType, vmInstance, "", 0, candidates);

        if (candidates.Count == 0)
        {
            EditorUtility.DisplayDialog("경로 없음",
                "IObservableProperty / IObservableList / ICommand / IAsyncCommand 타입 멤버를 찾지 못했습니다.",
                "확인");
            return;
        }

        var menu = new GenericMenu();
        foreach (var c in candidates)
        {
            bool selected = pathProp.stringValue == c.path;
            menu.AddItem(new GUIContent(c.label), selected, () =>
            {
                pathProp.stringValue = c.path;
                pathProp.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }

    // ==============
    // Context Lookup (ContextBindingBaseEditor.ResolveContext와 동일 정책)
    // ==============

    DataContextBase GetContextForEditor(ContextBindingBase binding)
    {
        var so       = new SerializedObject(binding);
        var modeProp = so.FindProperty("lookupMode");
        var nameProp = so.FindProperty("contextName");

        var mode = modeProp != null
            ? (ContextLookupMode)modeProp.enumValueIndex
            : ContextLookupMode.Nearest;
        var name = nameProp?.stringValue;

        switch (mode)
        {
            case ContextLookupMode.Nearest:
                return binding.GetComponentInParent<DataContextBase>();

            case ContextLookupMode.ByNameInParents:
                return FindContextInParentsByName(binding, name);

            case ContextLookupMode.ByNameInScene:
                return FindContextInSceneByName(name);

            default:
                return binding.GetComponentInParent<DataContextBase>();
        }
    }

    DataContextBase FindContextInParentsByName(ContextBindingBase binding, string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        var all = binding.GetComponentsInParent<DataContextBase>(includeInactive: true);
        foreach (var ctx in all)
        {
            if (ctx != null && ctx.ContextName == name)
                return ctx;
        }

        return null;
    }

    DataContextBase FindContextInSceneByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

#if UNITY_2022_2_OR_NEWER
        var all = UnityEngine.Object.FindObjectsByType<DataContextBase>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
#else
        var all = UnityEngine.Object.FindObjectsOfType<DataContextBase>(true);
#endif
        foreach (var ctx in all)
        {
            if (ctx != null && ctx.ContextName == name)
                return ctx;
        }

        return null;
    }

    // ==============
    // Path 후보 빌드 (ContextBindingBaseEditor.BuildCandidates 이식)
    // ==============

    void BuildCandidates(Type type, object instance, string basePath, int depth,
        List<(string path, string label)> acc)
    {
        if (depth > 4) // 너무 깊게 안 내려가도록
            return;

        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Public;

        foreach (var p in type.GetProperties(flags))
        {
            if (p.GetIndexParameters().Length > 0)
                continue;

            ProcessMember(p, p.PropertyType, obj => SafeGet(() => p.GetValue(obj)),
                type, instance, basePath, depth, acc);
        }

        foreach (var f in type.GetFields(flags))
        {
            ProcessMember(f, f.FieldType, obj => SafeGet(() => f.GetValue(obj)),
                type, instance, basePath, depth, acc);
        }
    }

    void ProcessMember(MemberInfo m, Type memberType,
        Func<object, object> mInstGetter,
        Type ownerType, object ownerInstance,
        string basePath, int depth,
        List<(string path, string label)> acc)
    {
        string name        = m.Name;
        string currentPath = string.IsNullOrEmpty(basePath) ? name : $"{basePath}.{name}";

        bool isObsProp  = typeof(IObservableProperty).IsAssignableFrom(memberType);
        bool isObsList  = typeof(IObservableList).IsAssignableFrom(memberType);
        bool isCommand  = typeof(ICommand).IsAssignableFrom(memberType);
        bool isAsyncCmd = typeof(IAsyncCommand).IsAssignableFrom(memberType);

        // 1) 바인딩 가능한 타입이면 바로 후보로 추가
        if (isObsProp || isObsList || isCommand || isAsyncCmd)
        {
            string typeName = memberType.Name;
            string label    = BuildDisplayLabel(currentPath, typeName, basePath);
            acc.Add((currentPath, label));
        }

        // 2) 딕셔너리(string 키)면 Stats["HP"] 같은 리프 추가 시도
        if (typeof(IDictionary).IsAssignableFrom(memberType))
        {
            // design-time instance가 있으면 실제 키들로 Stats["HP"] 형태 후보 추가
            if (ownerInstance != null)
            {
                var ownerVal = ownerInstance;
                var dictObj  = mInstGetter(ownerVal) as IDictionary;
                if (dictObj != null)
                {
                    foreach (DictionaryEntry entry in dictObj)
                    {
                        if (entry.Key is string keyStr)
                        {
                            string dictPath = $"{currentPath}[\"{keyStr}\"]";
                            string label    = $"{currentPath}/\"{keyStr}\"";
                            acc.Add((dictPath, label));
                        }
                    }
                }
            }
        }

        // 3) 복합 타입은 컨테이너로 보고 재귀 (단, UnityEngine.Object, primitive, string은 제외)
        if (!isObsProp && !isObsList && !isCommand && !isAsyncCmd)
        {
            if (IsContainerType(memberType))
            {
                object childInstance = null;
                if (ownerInstance != null)
                    childInstance = mInstGetter(ownerInstance);

                BuildCandidates(memberType, childInstance, currentPath, depth + 1, acc);
            }
        }
    }

    bool IsContainerType(Type t)
    {
        if (t.IsPrimitive || t.IsEnum)
            return false;
        if (t == typeof(string))
            return false;
        if (typeof(UnityEngine.Object).IsAssignableFrom(t))
            return false;
        if (typeof(IDictionary).IsAssignableFrom(t))
            return false;
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t != typeof(string))
            return false;

        return t.IsClass || t.IsValueType;
    }

    object SafeGet(Func<object> getter)
    {
        try { return getter(); }
        catch { return null; }
    }

    string BuildDisplayLabel(string fullPath, string typeName, string basePath)
    {
        // fullPath: "Stats.HP" → 메뉴 라벨을 "Stats/HP (ObservableProperty<int>)" 형태로
        string pathForMenu = fullPath.Replace('.', '/');
        return $"{pathForMenu} ({typeName})";
    }
}
#endif
