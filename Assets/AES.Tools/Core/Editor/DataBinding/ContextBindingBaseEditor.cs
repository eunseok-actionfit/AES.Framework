#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AES.Tools.Commands;
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    [CustomEditor(typeof(ContextBindingBase), true)]
    public class ContextBindingBaseEditor : UnityEditor.Editor
    {
        SerializedProperty _memberPathModeProp;
        SerializedProperty _memberPathProp;

        SerializedProperty _lookupModeProp;
        SerializedProperty _contextNameProp;

        protected virtual void OnEnable()
        {
            if (targets == null || targets.Length == 0 || targets[0] == null || target == null)
                return;
            
            _memberPathModeProp = serializedObject.FindProperty("memberPathMode");
            _memberPathProp = serializedObject.FindProperty("memberPath");

            _lookupModeProp = serializedObject.FindProperty("lookupMode");
            _contextNameProp = serializedObject.FindProperty("contextName");
        }

        public override void OnInspectorGUI()
        {
            // 여기도 동일하게 방어 (serializedObject.Update() 전에)
            if (targets == null || targets.Length == 0 || targets[0] == null || target == null)
                return;

            
            serializedObject.Update();

            // memberPath/mode 제외하고 나머지 기본 Inspector 먼저 그림
            DrawPropertiesExcluding(serializedObject,
                "lookupMode",
                "contextName",
                "memberPathMode",
                "memberPath");

            EditorGUILayout.Space();

            DrawContextSection();

            EditorGUILayout.Space();

            DrawMemberPathSection();

            serializedObject.ApplyModifiedProperties();
        }

        // --------------------------------------------------------------------
        // Context 영역
        // --------------------------------------------------------------------

        void DrawContextSection()
        {
            if (_lookupModeProp == null || _contextNameProp == null)
                return;

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

            var binding = (ContextBindingBase)target;
            MonoBehaviour[] providers = Array.Empty<MonoBehaviour>();

            switch (mode)
            {
                case ContextLookupMode.ByNameInParents:
                    providers = binding.GetComponentsInParent<MonoBehaviour>(includeInactive: true);
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
                    "현재 씬/부모 계층에서 사용할 수 있는 IBindingContextProvider 를 찾지 못했습니다.\n" +
                    "ContextName 은 수동으로 문자열을 입력해서 사용할 수 있습니다.",
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

                menu.AddItem(new GUIContent(contextName), selected, () => {
                    _contextNameProp.stringValue = contextName;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        // --------------------------------------------------------------------
        // Member Path 영역
        // --------------------------------------------------------------------

        void DrawMemberPathSection()
        {
            if (_memberPathModeProp == null || _memberPathProp == null)
                return;

            var binding = (ContextBindingBase)target;
            var provider = ResolveProvider(binding, out var mode, out var ctxNameForLookup);

            EditorGUILayout.LabelField("Member Path", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            DrawContextInfo(provider, mode, ctxNameForLookup);

            EditorGUILayout.PropertyField(_memberPathModeProp);

            var pathMode = (MemberPathMode)_memberPathModeProp.enumValueIndex;

            if (pathMode == MemberPathMode.Custom) { EditorGUILayout.PropertyField(_memberPathProp); }
            else
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.TextField("Path", _memberPathProp.stringValue);

                using (new EditorGUI.DisabledScope(provider == null))
                {
                    if (GUILayout.Button("Select...", GUILayout.Width(70)))
                        ShowPathMenu(provider);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        void DrawContextInfo(IBindingContextProvider provider, ContextLookupMode mode, string ctxNameForLookup)
        {
            if (provider is MonoBehaviour mb)
            {
                string ctxName;

                if (mb is MonoContext dc)
                    ctxName = dc.ContextName;
                else
                    ctxName = mb.gameObject.name;

                string label =
                    $"Provider: {mb.GetType().Name}  " +
                    $"(Name=\"{ctxName}\", Lookup={mode})";

                EditorGUILayout.HelpBox(label, MessageType.Info);
            }
            else
            {
                string msg = $"Context 해석 실패 (Lookup={mode}";
                if (mode != ContextLookupMode.Nearest)
                    msg += $", Name=\"{ctxNameForLookup}\"";

                msg += ")";

                EditorGUILayout.HelpBox(msg, MessageType.Warning);
            }
        }

        // --------------------------------------------------------------------
        // Provider Lookup (에디터용)
        // --------------------------------------------------------------------

        IBindingContextProvider ResolveProvider(
            ContextBindingBase binding,
            out ContextLookupMode mode,
            out string ctxNameForLookup)
        {
            mode = ContextLookupMode.Nearest;
            ctxNameForLookup = null;

            if (_lookupModeProp != null)
                mode = (ContextLookupMode)_lookupModeProp.enumValueIndex;

            if (_contextNameProp != null)
                ctxNameForLookup = _contextNameProp.stringValue;

            switch (mode)
            {
                case ContextLookupMode.Nearest:
                    return binding.GetComponentInParent<IBindingContextProvider>();

                case ContextLookupMode.ByNameInParents:
                    return FindProviderInParentsByName(binding, ctxNameForLookup);

                case ContextLookupMode.ByNameInScene:
                    return FindProviderInSceneByName(ctxNameForLookup);

                default:
                    return binding.GetComponentInParent<IBindingContextProvider>();
            }
        }

        IBindingContextProvider FindProviderInParentsByName(ContextBindingBase binding, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var all = binding.GetComponentsInParent<MonoBehaviour>(includeInactive: true);

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

        // --------------------------------------------------------------------
        // Path 후보 빌드 (기존 로직 재사용)
        // --------------------------------------------------------------------

        void ShowPathMenu(IBindingContextProvider provider)
        {
            if (provider == null)
            {
                EditorUtility.DisplayDialog("Context 없음",
                    "lookupMode 설정에 따라 IBindingContextProvider 를 찾지 못했습니다.", "확인");

                return;
            }

            var vmType = provider.DesignTimeViewModelType;
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
                string label = c.DisplayLabel;
                bool selected = _memberPathProp.stringValue == c.Path;

                menu.AddItem(new GUIContent(label), selected, () => {
                    _memberPathProp.stringValue = c.Path;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        class PathCandidate
        {
            public string Path;
            public string DisplayLabel;
        }

        void BuildCandidates(Type type, object instance, string basePath, int depth, List<PathCandidate> acc)
        {
            if (depth > 4)
                return;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public;

            foreach (var p in type.GetProperties(flags))
            {
                if (p.GetIndexParameters().Length > 0)
                    continue;

                ProcessMember(p, p.PropertyType, obj => SafeGet(() => p.GetValue(obj)), instance, basePath, depth, acc);
            }

            foreach (var f in type.GetFields(flags)) { ProcessMember(f, f.FieldType, obj => SafeGet(() => f.GetValue(obj)), instance, basePath, depth, acc); }
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
            string memberName = m.Name;
            string currentPath = string.IsNullOrEmpty(basePath) ? memberName : $"{basePath}.{memberName}";

            bool isObsProp = typeof(IObservableProperty).IsAssignableFrom(memberType);
            bool isObsList = typeof(IObservableList).IsAssignableFrom(memberType);
            bool isCommand = typeof(ICommand).IsAssignableFrom(memberType);
            bool isAsyncCmd = typeof(IAsyncCommand).IsAssignableFrom(memberType);

            bool hasBindingAttr =
                m.IsDefined(typeof(BindableAttribute), inherit: true);

            // 0) IDictionary<string, T> 같은 경우 바로 키들 추가 (기존 로직 유지)
            if (typeof(IDictionary).IsAssignableFrom(memberType))
            {
                if (ownerInstance != null)
                {
                    var ownerVal = ownerInstance;
                    var dictObj = mInstGetter(ownerVal) as IDictionary;

                    if (dictObj != null)
                    {
                        foreach (DictionaryEntry entry in dictObj)
                        {
                            if (entry.Key is string keyStr)
                            {
                                string dictPath = $"{currentPath}[\"{keyStr}\"]";
                                string label = $"{currentPath}/\"{keyStr}\"";

                                acc.Add(new PathCandidate
                                {
                                    Path = dictPath,
                                    DisplayLabel = label
                                });
                            }
                        }
                    }
                }
            }

            // 1) 자기 자신을 후보에 추가 (기존과 동일)
            if (isObsProp || isObsList || isCommand || isAsyncCmd || hasBindingAttr)
            {
                string typeName = GetFriendlyTypeName(memberType);
                string label = BuildDisplayLabel(currentPath, typeName);

                acc.Add(new PathCandidate
                {
                    Path = currentPath,
                    DisplayLabel = label
                });
            }

            // 2-A) 일반 컨테이너 타입이면 기존처럼 재귀
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
            // 2-B) IObservableProperty 인 경우: Value 타입 안으로 한 번 더 들어가기
            else if (isObsProp)
            {
                // Value 프로퍼티 타입을 기준으로 내려감
                var valuePropInfo = memberType.GetProperty(
                    "Value",
                    BindingFlags.Instance | BindingFlags.Public);

                if (valuePropInfo != null)
                {
                    var valueType = valuePropInfo.PropertyType;

                    if (IsContainerType(valueType))
                    {
                        object valueInstance = null;

                        if (ownerInstance != null)
                        {
                            var obsObj = mInstGetter(ownerInstance);

                            if (obsObj != null) { valueInstance = SafeGet(() => valuePropInfo.GetValue(obsObj)); }
                        }

                        // Path: CurrentLevel.Value.xxx ...
                        string valuePath = $"{currentPath}.Value";

                        BuildCandidates(valueType, valueInstance, valuePath, depth + 1, acc);
                    }
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

            if (typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string))
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

        static readonly Dictionary<Type, string> s_builtinNames = new()
        {
            { typeof(float),   "float" },
            { typeof(double),  "double" },
            { typeof(int),     "int" },
            { typeof(uint),    "uint" },
            { typeof(long),    "long" },
            { typeof(ulong),   "ulong" },
            { typeof(short),   "short" },
            { typeof(ushort),  "ushort" },
            { typeof(byte),    "byte" },
            { typeof(sbyte),   "sbyte" },
            { typeof(bool),    "bool" },
            { typeof(char),    "char" },
            { typeof(string),  "string" },
            { typeof(decimal), "decimal" },
            { typeof(void),    "void" },
        };
        
        string GetFriendlyTypeName(Type t)
        {
            if (t == null)
                return "null";

            // 1) 기본 타입: float / int / bool / string 등
            if (s_builtinNames.TryGetValue(t, out var alias))
                return alias;

            // 2) 배열
            if (t.IsArray)
            {
                var elemType = t.GetElementType();
                return $"{GetFriendlyTypeName(elemType)}[]";
            }

            // 3) 제네릭이 아닌 타입
            if (!t.IsGenericType)
                return t.Name;

            // 4) 제네릭 타입: ObservableProperty<float> 등
            var genericDef = t.IsGenericTypeDefinition ? t : t.GetGenericTypeDefinition();

            // 필요하면 ObservableProperty/IObservableProperty를 "Observable"로 축약
            string genericName;
            if (genericDef.Name.StartsWith("ObservableProperty")
                || genericDef.Name.StartsWith("IObservableProperty"))
            {
                genericName = "Observable";
            }
            else
            {
                genericName = t.Name;
                int backtickIndex = genericName.IndexOf('`');
                if (backtickIndex >= 0)
                    genericName = genericName.Substring(0, backtickIndex);
            }

            var args     = t.GetGenericArguments();
            var argNames = new string[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                // 여기서 다시 GetFriendlyTypeName 호출 → float → "float" 로 변환됨
                argNames[i] = GetFriendlyTypeName(args[i]);
            }

            return $"{genericName}<{string.Join(", ", argNames)}>";
        }

    }
}
#endif