#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AES.Tools.Editor.Util;
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

        // InheritFromParent 설정
        SerializedProperty _inheritLookupModeProp;
        SerializedProperty _inheritContextNameProp;
        SerializedProperty _inheritMemberPathProp;
        
        static bool _showRuntimeDebug = false;

        void OnEnable()
        {
            _nameModeProp           = serializedObject.FindProperty("nameMode");
            _customNameProp         = serializedObject.FindProperty("customName");
            _viewModelSourceProp    = serializedObject.FindProperty("viewModelSource");
            _viewModelTypeNameProp  = serializedObject.FindProperty("viewModelTypeName");

            _inheritLookupModeProp  = serializedObject.FindProperty("inheritLookupMode");
            _inheritContextNameProp = serializedObject.FindProperty("inheritContextName");
            _inheritMemberPathProp  = serializedObject.FindProperty("inheritMemberPath");
        }

        public override void OnInspectorGUI()
        {
            if (target == null)
                return;

            serializedObject.Update();

            // ------------------------------
            // 기본 Context 설정
            // ------------------------------
            EditorGUILayout.PropertyField(_nameModeProp);
            var nameMode = (ContextNameMode)_nameModeProp.enumValueIndex;
            if (nameMode == ContextNameMode.Custom)
                EditorGUILayout.PropertyField(_customNameProp);

            EditorGUILayout.PropertyField(_viewModelSourceProp);
            var sourceMode = (ViewModelSourceMode)_viewModelSourceProp.enumValueIndex;

            EditorGUILayout.Space(6);

            // ------------------------------
            // ViewModel Type 선택 (AutoCreate / External 용)
            // ------------------------------
            DrawViewModelTypeField();

            EditorGUILayout.Space(10);

            // ------------------------------
            // InheritFromParent 전용 설정
            // ------------------------------
            if (sourceMode == ViewModelSourceMode.InheritFromParent)
            {
                EditorGUILayout.LabelField("Inherit From Parent Settings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                DrawInheritContextSection();
                EditorGUILayout.Space(4);
                DrawInheritMemberPathSection();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(8);
            }

            // ------------------------------
            // HelpBox
            // ------------------------------
            if (MenuHelp.HelpEnabled)
            {
                EditorGUILayout.HelpBox(
                    "• ViewModel Type은 Path Binding 드롭다운(디자인타임)에서 사용하는 타입입니다.\n" +
                    "• AutoCreate 모드에서는 해당 타입으로 ViewModel 인스턴스를 생성합니다.\n" +
                    "• External 모드에서는 Presenter/Service에서 SetViewModel()로 수동 지정해야 합니다.\n" +
                    "• InheritFromParent 모드는 부모 Context 의 특정 멤버(ChildVm 등)를 서브 컨텍스트 루트로 사용합니다.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // ------------------------------
            // Runtime Debug 패널
            // ------------------------------
            DrawRuntimeDebugSection();

            serializedObject.ApplyModifiedProperties();
        }

        // --------------------------------------------------------------------
        // ViewModel Type
        // --------------------------------------------------------------------

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
                    !a.FullName.StartsWith("System", StringComparison.Ordinal) &&
                    !a.FullName.StartsWith("Unity", StringComparison.Ordinal))
                .SelectMany(a => {
                    try { return a.GetTypes(); }
                    catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
                })
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    (t.Name.EndsWith("ViewModel", StringComparison.Ordinal) ||
                     t.Name.EndsWith("VM", StringComparison.Ordinal))
                )
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
                menu.AddItem(new GUIContent(display), false, () => {
                    _viewModelTypeNameProp.stringValue = t.AssemblyQualifiedName;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        // --------------------------------------------------------------------
        // Inherit Context 선택
        // --------------------------------------------------------------------

        void DrawInheritContextSection()
        {
            if (_inheritLookupModeProp == null || _inheritContextNameProp == null)
                return;

            var self = (MonoContext)target;

            EditorGUILayout.LabelField("Parent Context", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_inheritLookupModeProp, new GUIContent("Lookup Mode"));
            var mode = (ContextLookupMode)_inheritLookupModeProp.enumValueIndex;

            if (mode == ContextLookupMode.Nearest)
            {
                EditorGUILayout.HelpBox(
                    "가장 가까운 상위 IBindingContextProvider 를 부모로 사용합니다.",
                    MessageType.Info);

                EditorGUI.indentLevel--;
                return;
            }

            MonoBehaviour[] providers = CollectProvidersForLookup(self, mode);
            var nameList = new List<string>();

            foreach (var mb in providers)
            {
                // ★ 자기 자신은 스킵
                if (mb == self)
                    continue;

                if (mb is IBindingContextProvider)
                {
                    string logicalName =
                        mb is MonoContext dc
                            ? dc.ContextName
                            : mb.gameObject.name;

                    if (!string.IsNullOrEmpty(logicalName) && !nameList.Contains(logicalName))
                        nameList.Add(logicalName);
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_inheritContextNameProp, new GUIContent("Context Name"));

            using (new EditorGUI.DisabledScope(nameList.Count == 0))
            {
                if (GUILayout.Button("Select...", GUILayout.Width(70)))
                    ShowInheritContextNameMenu(nameList);
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

        static MonoBehaviour[] CollectProvidersForLookup(MonoContext self, ContextLookupMode mode)
        {
            switch (mode)
            {
                case ContextLookupMode.ByNameInParents:
                    return self.GetComponentsInParent<MonoBehaviour>(includeInactive: true);

                case ContextLookupMode.ByNameInScene:
#if UNITY_2022_2_OR_NEWER
                    return FindObjectsByType<MonoBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
#else
                    return UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
#endif
                default:
                    return Array.Empty<MonoBehaviour>();
            }
        }

        void ShowInheritContextNameMenu(List<string> nameList)
        {
            if (nameList == null || nameList.Count == 0)
                return;

            var menu = new GenericMenu();

            foreach (var contextName in nameList)
            {
                bool selected = _inheritContextNameProp.stringValue == contextName;

                menu.AddItem(new GUIContent(contextName), selected, () => {
                    _inheritContextNameProp.stringValue = contextName;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        // --------------------------------------------------------------------
        // Inherit Member Path (SubContext 루트 경로)
        // --------------------------------------------------------------------

        void DrawInheritMemberPathSection()
        {
            if (_inheritMemberPathProp == null)
                return;

            var (provider, mode, ctxNameForLookup) = ResolveParentProviderForEditor();

            EditorGUILayout.LabelField("Base Member Path", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            DrawContextInfo(provider, mode, ctxNameForLookup);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_inheritMemberPathProp, new GUIContent("Path"));

            using (new EditorGUI.DisabledScope(provider == null))
            {
                if (GUILayout.Button("Select...", GUILayout.Width(70)))
                    ShowInheritPathMenu(provider);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        (IBindingContextProvider provider, ContextLookupMode mode, string ctxNameForLookup)
            ResolveParentProviderForEditor()
        {
            var self = (MonoContext)target;

            var mode = (ContextLookupMode)_inheritLookupModeProp.enumValueIndex;
            string ctxNameForLookup = _inheritContextNameProp.stringValue;

            IBindingContextProvider provider = null;

            switch (mode)
            {
                case ContextLookupMode.Nearest:
                    provider = GetNearestProviderInParents(self);
                    break;

                case ContextLookupMode.ByNameInParents:
                    provider = FindProviderInParentsByName(self, ctxNameForLookup);
                    break;

                case ContextLookupMode.ByNameInScene:
                    provider = FindProviderInSceneByName(ctxNameForLookup, self);
                    break;
            }

            return (provider, mode, ctxNameForLookup);
        }

        static IBindingContextProvider GetNearestProviderInParents(MonoContext self)
        {
            // GetComponentsInParent 가 자기 자신을 포함할 수 있으므로 반드시 self 스킵
            var parents = self.GetComponentsInParent<MonoBehaviour>(includeInactive: true);

            foreach (var mb in parents)
            {
                if (mb == self)
                    continue;

                if (mb is IBindingContextProvider p)
                    return p;
            }

            return null;
        }

        static IBindingContextProvider FindProviderInParentsByName(MonoContext self, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var parents = self.GetComponentsInParent<MonoBehaviour>(includeInactive: true);

            foreach (var mb in parents)
            {
                // ★ 자기 자신 스킵
                if (mb == self)
                    continue;

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

        static IBindingContextProvider FindProviderInSceneByName(string name, MonoContext self)
        {
            if (string.IsNullOrEmpty(name))
                return null;

#if UNITY_2022_2_OR_NEWER
            var all = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
            var all = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>(true);
#endif
            foreach (var mb in all)
            {
                // ★ 자기 자신 스킵
                if (mb == self)
                    continue;

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

        void DrawContextInfo(IBindingContextProvider provider, ContextLookupMode mode, string ctxNameForLookup)
        {
            if (provider is MonoBehaviour mb)
            {
                string ctxName =
                    mb is MonoContext dc
                        ? dc.ContextName
                        : mb.gameObject.name;

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
        // Runtime Debug
        // --------------------------------------------------------------------
        void DrawRuntimeDebugSection()
        {
            var ctx = (MonoContext)target;
            var sourceMode = (ViewModelSourceMode)_viewModelSourceProp.enumValueIndex;

            // 전역 디버그가 꺼져 있으면 아예 안 보이게
            if (!BindingDebugSettings.Enabled)
                return;

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _showRuntimeDebug = EditorGUILayout.Foldout(
                    _showRuntimeDebug,
                    "Runtime Debug",
                    true);

                if (!_showRuntimeDebug)
                    return;

                EditorGUI.indentLevel++;

                if (!EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox(
                        "플레이 모드에서 ViewModel 생성/주입/상속 상태를 확인할 수 있습니다.",
                        MessageType.Info);
                    EditorGUI.indentLevel--;
                    return;
                }

                EditorGUILayout.LabelField("Source Mode", sourceMode.ToString());
                EditorGUILayout.LabelField("Context Name", ctx.ContextName);

                var vmType    = ctx.ViewModelType;
                var vmInst    = ctx.ViewModel;
                var runtimeCtx = ctx.RuntimeContext;

                EditorGUILayout.LabelField("ViewModel Type",
                    vmType != null ? vmType.FullName : "(null)");

                EditorGUILayout.LabelField("ViewModel Instance",
                    vmInst != null ? vmInst.ToString() : "(null)");

                EditorGUILayout.LabelField("RuntimeContext",
                    runtimeCtx != null ? runtimeCtx.GetType().Name : "(null)");

                if (sourceMode == ViewModelSourceMode.External)
                {
                    if (vmInst == null)
                        EditorGUILayout.HelpBox(
                            "External 모드인데 ViewModel 인스턴스가 아직 없습니다.\n" +
                            "→ Presenter/EntryPoint에서 SetViewModel()이 호출되지 않았을 가능성이 있습니다.",
                            MessageType.Warning);
                }
                else if (sourceMode == ViewModelSourceMode.AutoCreate)
                {
                    if (vmInst == null)
                    {
                        EditorGUILayout.HelpBox(
                            "AutoCreate 모드인데 ViewModel 인스턴스가 없습니다.\n" +
                            "→ viewModelTypeName 설정 또는 Activator.CreateInstance 실패 여부를 확인하세요.",
                            MessageType.Error);
                    }
                }
                else if (sourceMode == ViewModelSourceMode.InheritFromParent)
                {
                    var (provider, mode, ctxNameForLookup) = ResolveParentProviderForEditor();
                    string basePath = _inheritMemberPathProp != null
                        ? _inheritMemberPathProp.stringValue
                        : "";

                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("InheritFromParent Runtime Info", EditorStyles.boldLabel);

                    if (provider is MonoBehaviour mb)
                    {
                        string parentName =
                            mb is MonoContext dc ? dc.ContextName : mb.gameObject.name;

                        EditorGUILayout.LabelField("Parent Provider", $"{mb.GetType().Name} ({parentName})");
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Parent Provider", "(null)");
                    }

                    EditorGUILayout.LabelField("Lookup Mode", mode.ToString());
                    EditorGUILayout.LabelField("Inherit ContextName",
                        string.IsNullOrEmpty(ctxNameForLookup) ? "(empty)" : ctxNameForLookup);
                    EditorGUILayout.LabelField("Base Path",
                        string.IsNullOrEmpty(basePath) ? "(root)" : basePath);

                    if (runtimeCtx == null)
                    {
                        EditorGUILayout.HelpBox(
                            "상속용 RuntimeContext 가 아직 null 입니다.\n" +
                            "→ 부모 ViewModel 주입 이전이거나, 상속 컨텍스트가 아직 준비되지 않았을 수 있습니다.",
                            MessageType.Warning);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }


        // --------------------------------------------------------------------
        // Path 후보 빌드 ([Bindable]만 노출)
        // --------------------------------------------------------------------

        void ShowInheritPathMenu(IBindingContextProvider provider)
        {
            if (provider == null)
            {
                EditorUtility.DisplayDialog(
                    "Context 없음",
                    "inheritLookupMode 설정에 따라 IBindingContextProvider 를 찾지 못했습니다.",
                    "확인");

                return;
            }

            var vmType = provider.DesignTimeViewModelType;
            var vmInstance = provider.GetDesignTimeViewModel();

            if (vmType == null)
            {
                EditorUtility.DisplayDialog(
                    "ViewModel 타입 없음",
                    "Design-time ViewModel 타입을 알 수 없습니다.",
                    "확인");

                return;
            }

            var candidates = new List<PathCandidate>();
            BuildCandidates(vmType, vmInstance, "", 0, candidates);

            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "경로 없음",
                    "[Bindable] 멤버를 찾지 못했습니다.",
                    "확인");

                return;
            }

            var menu = new GenericMenu();

            foreach (var c in candidates)
            {
                string label = c.DisplayLabel;
                bool selected = _inheritMemberPathProp.stringValue == c.Path;

                menu.AddItem(new GUIContent(label), selected, () => {
                    _inheritMemberPathProp.stringValue = c.Path;
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
            if (depth > 4 || type == null)
                return;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public;

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
            string memberName = m.Name;
            string currentPath = string.IsNullOrEmpty(basePath) ? memberName : $"{basePath}.{memberName}";

            bool isObsProp = typeof(IObservableProperty).IsAssignableFrom(memberType);

            bool hasBindingAttr =
                m.IsDefined(typeof(BindableAttribute), inherit: true);

            // IDictionary<string, T> 이면서 [Bindable]인 경우: 키들을 후보로 추가
            if (typeof(IDictionary).IsAssignableFrom(memberType) && hasBindingAttr)
            {
                if (ownerInstance != null)
                {
                    var ownerVal = ownerInstance;

                    if (mInstGetter(ownerVal) is IDictionary dictObj)
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

            // 1) 오직 [Bindable] 멤버만 후보에 추가
            if (hasBindingAttr)
            {
                string typeName = memberType.GetDisplayName();  
                string label = BuildDisplayLabel(currentPath, typeName);

                acc.Add(new PathCandidate
                {
                    Path = currentPath,
                    DisplayLabel = label
                });
            }

            // 2-A) ObservableProperty 인 경우: Value 타입 안으로 내려가서 내부 [Bindable] 탐색
            if (isObsProp)
            {
                var valuePropInfo = memberType.GetProperty("Value",
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

                            if (obsObj != null)
                                valueInstance = SafeGet(() => valuePropInfo.GetValue(obsObj));
                        }

                        string valuePath = $"{currentPath}.Value";

                        // Value 자체는 Bindable이 아니면 후보에 안 올리고,
                        // 내부로만 내려가서 [Bindable] 멤버를 찾는다.
                        BuildCandidates(valueType, valueInstance, valuePath, depth + 1, acc);
                    }
                }
            }
            // 2-B) 일반 컨테이너 타입이면 재귀 (Bindable 여부와 무관)
            else
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
            if (t == null)
                return false;

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
    }
}
#endif