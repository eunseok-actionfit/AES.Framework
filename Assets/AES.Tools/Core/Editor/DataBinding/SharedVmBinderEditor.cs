// SharedVmBinderEditor.cs
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AES.Tools.Commands;
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor.DataBinding
{
    [CustomEditor(typeof(SharedVmBinder))]
    public class SharedVmBinderEditor : UnityEditor.Editor
    {
        SerializedProperty _lookupModeProp;
        SerializedProperty _contextNameProp;
        SerializedProperty _entriesProp;

        void OnEnable()
        {
            _lookupModeProp  = serializedObject.FindProperty("lookupMode");
            _contextNameProp = serializedObject.FindProperty("contextName");
            _entriesProp     = serializedObject.FindProperty("entries");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawContextSection();
            EditorGUILayout.Space(8);

            DrawEntriesSection();

            serializedObject.ApplyModifiedProperties();
        }

        // ────────────────────────────────────────
        // Context 선택 영역 (ContextBindingBaseEditor 로직 재사용) :contentReference[oaicite:6]{index=6}
        // ────────────────────────────────────────
        void DrawContextSection()
        {
            EditorGUILayout.LabelField("Context", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_lookupModeProp);
            var mode = (ContextLookupMode)_lookupModeProp.enumValueIndex;

            if (mode == ContextLookupMode.Nearest)
            {
                EditorGUILayout.HelpBox("가장 가까운 상위 IBindingContextProvider 를 사용합니다.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            var binder = (SharedVmBinder)target;
            MonoBehaviour[] providers = Array.Empty<MonoBehaviour>();

            switch (mode)
            {
                case ContextLookupMode.ByNameInParents:
                    providers = binder.GetComponentsInParent<MonoBehaviour>(true);
                    break;

                case ContextLookupMode.ByNameInScene:
#if UNITY_2022_2_OR_NEWER
                    providers = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
#else
                    providers = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
#endif
                    break;
            }

            var nameList = new List<string>();

            foreach (var mb in providers)
            {
                if (mb is IBindingContextProvider)
                {
                    string logicalName;

                    if (mb is MonoContext dc)
                        logicalName = dc.ContextName;
                    else
                        logicalName = mb.gameObject.name;

                    if (!string.IsNullOrEmpty(logicalName) && !nameList.Contains(logicalName))
                        nameList.Add(logicalName);
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_contextNameProp, new GUIContent("Context Name"));

            using (new EditorGUI.DisabledScope(nameList.Count == 0))
            {
                if (GUILayout.Button("Select...", GUILayout.Width(70)))
                    ShowContextNameMenu(nameList);
            }

            EditorGUILayout.EndHorizontal();

            if (nameList.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "사용 가능한 IBindingContextProvider 를 찾지 못했습니다.\n" +
                    "ContextName 은 문자열로 수동 입력할 수 있습니다.",
                    MessageType.Warning);
            }

            EditorGUI.indentLevel--;
        }

        void ShowContextNameMenu(List<string> nameList)
        {
            if (nameList == null || nameList.Count == 0)
                return;

            var menu = new GenericMenu();

            foreach (var contextName in nameList)
            {
                bool selected = _contextNameProp.stringValue == contextName;

                menu.AddItem(new GUIContent(contextName), selected, () =>
                {
                    _contextNameProp.stringValue = contextName;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        // ────────────────────────────────────────
        // Entries 영역
        // ────────────────────────────────────────
        void DrawEntriesSection()
        {
            EditorGUILayout.LabelField("Shared ↔ VM Bindings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Shared Fields"))
                ScanSharedFields();
            if (GUILayout.Button("Add Entry"))
                ShowAddEntryMenu();
            if (GUILayout.Button("Clear"))
                _entriesProp.ClearArray();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            for (int i = 0; i < _entriesProp.arraySize; i++)
            {
                var eProp = _entriesProp.GetArrayElementAtIndex(i);
                DrawEntry(eProp, i);
                EditorGUILayout.Space(2);
            }
        }

        void ScanSharedFields()
        {
            var binder   = (SharedVmBinder)target;
            var go       = binder.gameObject;
            var comps    = go.GetComponents<MonoBehaviour>();

            // 기존 엔트리 삭제 후 새 스캔
            _entriesProp.ClearArray();

            int index = 0;
            foreach (var comp in comps)
            {
                if (!comp) continue;

                var t = comp.GetType();
                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    if (!f.FieldType.IsGenericType ||
                        f.FieldType.GetGenericTypeDefinition() != typeof(Shared<>))
                        continue;

                    _entriesProp.InsertArrayElementAtIndex(index);
                    var eProp   = _entriesProp.GetArrayElementAtIndex(index);
                    var ownerP  = eProp.FindPropertyRelative("owner");
                    var nameP   = eProp.FindPropertyRelative("sharedFieldName");
                    var bindP   = eProp.FindPropertyRelative("binding");

                    ownerP.objectReferenceValue = comp;
                    nameP.stringValue           = f.Name;

                    // binding 은 vmPath 비워둔 상태로 두고, 나중에 Path 드롭다운에서 선택
                    var vmPathProp  = bindP.FindPropertyRelative("vmPath");
                    vmPathProp.stringValue = "";

                    index++;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
        
        void ShowAddEntryMenu()
        {
            var binder = (SharedVmBinder)target;
            var go     = binder.gameObject;

            var comps  = go.GetComponents<MonoBehaviour>();
            var menu   = new GenericMenu();

            bool found = false;

            foreach (var comp in comps)
            {
                if (!comp) continue;

                var t = comp.GetType();
                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var f in fields)
                {
                    if (!f.FieldType.IsGenericType ||
                        f.FieldType.GetGenericTypeDefinition() != typeof(Shared<>))
                        continue;

                    found = true;

                    string display = $"{comp.GetType().Name}/{f.Name}";

                    menu.AddItem(new GUIContent(display), false, () =>
                    {
                        AddEntry(comp, f.Name);
                    });
                }
            }

            if (!found)
                menu.AddDisabledItem(new GUIContent("No Shared<T> fields found"));

            menu.ShowAsContext();
        }

        void AddEntry(MonoBehaviour owner, string fieldName)
        {
            _entriesProp.arraySize++;
            var eProp = _entriesProp.GetArrayElementAtIndex(_entriesProp.arraySize - 1);

            var ownerP  = eProp.FindPropertyRelative("owner");
            var nameP   = eProp.FindPropertyRelative("sharedFieldName");
            var bindP   = eProp.FindPropertyRelative("binding");
            var vmPathP = bindP.FindPropertyRelative("vmPath");

            ownerP.objectReferenceValue = owner;
            nameP.stringValue           = fieldName;
            vmPathP.stringValue         = "";   // 초기 VM path 비우기

            serializedObject.ApplyModifiedProperties();
        }


        void DrawEntry(SerializedProperty entryProp, int index)
        {
            var ownerProp  = entryProp.FindPropertyRelative("owner");
            var fieldNameP = entryProp.FindPropertyRelative("sharedFieldName");
            var bindingP   = entryProp.FindPropertyRelative("binding");

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Entry {index}", EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                _entriesProp.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(ownerProp);

            var owner = ownerProp.objectReferenceValue as MonoBehaviour;

            // Shared<T> 필드 드롭다운
            using (new EditorGUI.DisabledScope(owner == null))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Shared Field");

                if (owner == null)
                {
                    EditorGUILayout.LabelField("(Owner 없음)");
                }
                else
                {
                    if (GUILayout.Button(string.IsNullOrEmpty(fieldNameP.stringValue)
                            ? "(Select Shared<T> field)"
                            : fieldNameP.stringValue,
                        EditorStyles.popup))
                    {
                        ShowSharedFieldMenu(owner, fieldNameP);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(2);

            // VM 경로 + 방향
            DrawVmBinding(bindingP);

            EditorGUILayout.EndVertical();
        }

        void ShowSharedFieldMenu(MonoBehaviour owner, SerializedProperty fieldNameProp)
        {
            var menu = new GenericMenu();
            var t    = owner.GetType();
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            bool any = false;
            foreach (var f in fields)
            {
                if (!f.FieldType.IsGenericType ||
                    f.FieldType.GetGenericTypeDefinition() != typeof(Shared<>))
                    continue;

                any = true;
                string name = f.Name;
                bool selected = fieldNameProp.stringValue == name;

                menu.AddItem(new GUIContent(name), selected, () =>
                {
                    fieldNameProp.stringValue = name;
                    fieldNameProp.serializedObject.ApplyModifiedProperties();
                });
            }

            if (!any)
                menu.AddDisabledItem(new GUIContent("No Shared<T> fields"));

            menu.ShowAsContext();
        }

        void DrawVmBinding(SerializedProperty bindingProp)
        {
            var vmPathProp = bindingProp.FindPropertyRelative("vmPath");
            var dirProp    = bindingProp.FindPropertyRelative("direction");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("VM Path", GUILayout.Width(70));

            EditorGUILayout.TextField(vmPathProp.stringValue);

            if (GUILayout.Button("Select...", GUILayout.Width(70)))
            {
                ShowVmPathMenu(vmPathProp);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(dirProp);
        }

        // ContextBindingBaseEditor의 ShowPathMenu / BuildCandidates 재활용 :contentReference[oaicite:7]{index=7}
        void ShowVmPathMenu(SerializedProperty vmPathProp)
        {
            var binder   = (SharedVmBinder)target;
            var provider = ResolveProviderForEditor(binder, out var mode, out var name);

            if (provider == null)
            {
                EditorUtility.DisplayDialog("Context 없음",
                    "lookupMode/ContextName 설정에 따라 IBindingContextProvider 를 찾지 못했습니다.", "확인");
                return;
            }

            var vmType     = provider.DesignTimeViewModelType;
            var vmInstance = provider.GetDesignTimeViewModel();

            if (vmType == null)
            {
                EditorUtility.DisplayDialog("ViewModel 타입 없음",
                    "Design-time ViewModel 타입을 알 수 없습니다.", "확인");
                return;
            }

            var candidates = new List<PathCandidate>();
            BuildCandidates(vmType, vmInstance, "", 0, candidates);

            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog("경로 없음",
                    "IObservableProperty / IObservableList / ICommand / [Bindable] 멤버를 찾지 못했습니다.", "확인");
                return;
            }

            var menu = new GenericMenu();
            foreach (var c in candidates)
            {
                bool selected = vmPathProp.stringValue == c.Path;
                menu.AddItem(new GUIContent(c.DisplayLabel), selected, () =>
                {
                    vmPathProp.stringValue = c.Path;
                    vmPathProp.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        // ContextBindingBaseEditor.ResolveProvider와 동일 로직 (에디터 전용) :contentReference[oaicite:8]{index=8}
        IBindingContextProvider ResolveProviderForEditor(
            SharedVmBinder binder,
            out ContextLookupMode mode,
            out string ctxNameForLookup)
        {
            mode             = (ContextLookupMode)_lookupModeProp.enumValueIndex;
            ctxNameForLookup = _contextNameProp.stringValue;

            switch (mode)
            {
                case ContextLookupMode.Nearest:
                    return binder.GetComponentInParent<IBindingContextProvider>();

                case ContextLookupMode.ByNameInParents:
                    return FindProviderInParentsByName(binder, ctxNameForLookup);

                case ContextLookupMode.ByNameInScene:
                    return FindProviderInSceneByName(ctxNameForLookup);

                default:
                    return binder.GetComponentInParent<IBindingContextProvider>();
            }
        }

        IBindingContextProvider FindProviderInParentsByName(SharedVmBinder binder, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var all = binder.GetComponentsInParent<MonoBehaviour>(true);
            foreach (var mb in all)
            {
                if (mb is IBindingContextProvider p)
                {
                    if (mb is MonoContext dc && dc.ContextName == name)
                        return p;

                    if (mb.gameObject.name == name)
                        return p;
                }
            }

            return null;
        }

        IBindingContextProvider FindProviderInSceneByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

#if UNITY_2022_2_OR_NEWER
            var all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
            var all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
#endif
            foreach (var mb in all)
            {
                if (mb is IBindingContextProvider p)
                {
                    if (mb is MonoContext dc && dc.ContextName == name)
                        return p;

                    if (mb.gameObject.name == name)
                        return p;
                }
            }

            return null;
        }

        // ────────────────────────────────────────
        // PathCandidate / BuildCandidates (ContextBindingBaseEditor와 동일) :contentReference[oaicite:9]{index=9}
        // ────────────────────────────────────────

        class PathCandidate
        {
            public string Path;
            public string DisplayLabel;
        }

        void BuildCandidates(Type type, object instance, string basePath, int depth, List<PathCandidate> acc)
        {
            if (depth > 4)
                return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            foreach (var p in type.GetProperties(flags))
            {
                if (p.GetIndexParameters().Length > 0)
                    continue;

                ProcessMember(p, p.PropertyType, obj => SafeGet(() => p.GetValue(obj)), instance, basePath, depth, acc);
            }

            foreach (var f in type.GetFields(flags))
            {
                ProcessMember(f, f.FieldType, obj => SafeGet(() => f.GetValue(obj)), instance, basePath, depth, acc);
            }
        }

        void ProcessMember(
            MemberInfo m,
            Type memberType,
            Func<object, object> mInstGetter,
            object ownerInstance,
            string basePath,
            int depth,
            List<PathCandidate> acc)
        {
            string memberName  = m.Name;
            string currentPath = string.IsNullOrEmpty(basePath) ? memberName : $"{basePath}.{memberName}";

            bool isObsProp  = typeof(IObservableProperty).IsAssignableFrom(memberType);
            bool isObsList  = typeof(IObservableList).IsAssignableFrom(memberType);
            bool isCommand  = typeof(ICommand).IsAssignableFrom(memberType);
            bool isAsyncCmd = typeof(IAsyncCommand).IsAssignableFrom(memberType);

            bool hasBindingAttr =
                m.IsDefined(typeof(BindableAttribute), inherit: true);

            if (isObsProp || isObsList || isCommand || isAsyncCmd || hasBindingAttr)
            {
                string typeName = GetFriendlyTypeName(memberType);
                string label    = BuildDisplayLabel(currentPath, typeName);

                acc.Add(new PathCandidate
                {
                    Path         = currentPath,
                    DisplayLabel = label
                });
            }

            if (typeof(IDictionary).IsAssignableFrom(memberType))
            {
                if (ownerInstance != null)
                {
                    var dictObj = mInstGetter(ownerInstance) as IDictionary;
                    if (dictObj != null)
                    {
                        foreach (DictionaryEntry entry in dictObj)
                        {
                            if (entry.Key is string keyStr)
                            {
                                string dictPath = $"{currentPath}[\"{keyStr}\"]";
                                string label    = $"{currentPath}/\"{keyStr}\"";

                                acc.Add(new PathCandidate
                                {
                                    Path         = dictPath,
                                    DisplayLabel = label
                                });
                            }
                        }
                    }
                }
            }

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

        string BuildDisplayLabel(string fullPath, string typeName)
        {
            string pathForMenu = fullPath.Replace('.', '/');
            return $"{pathForMenu} ({typeName})";
        }

        string GetFriendlyTypeName(Type t)
        {
            if (t == null)
                return "null";

            if (!t.IsGenericType)
                return t.Name;

            string genericName = t.Name;
            int backtickIndex  = genericName.IndexOf('`');
            if (backtickIndex >= 0)
                genericName = genericName.Substring(0, backtickIndex);

            var args     = t.GetGenericArguments();
            var argNames = new string[args.Length];

            for (int i = 0; i < args.Length; i++)
                argNames[i] = GetFriendlyTypeName(args[i]);

            return $"{genericName}<{string.Join(", ", argNames)}>";
        }
    }
}
#endif
