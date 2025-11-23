#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AES.Tools;
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

        void OnEnable()
        {
            _memberPathModeProp = serializedObject.FindProperty("memberPathMode");
            _memberPathProp     = serializedObject.FindProperty("memberPath");

            _lookupModeProp     = serializedObject.FindProperty("lookupMode");
            _contextNameProp    = serializedObject.FindProperty("contextName");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // memberPath/mode 제외하고 나머지 기본 Inspector 먼저 그림
            DrawPropertiesExcluding(serializedObject,
                "memberPathMode",
                "memberPath");

            EditorGUILayout.Space();
            DrawMemberPathSection();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawMemberPathSection()
        {
            if (_memberPathModeProp == null || _memberPathProp == null)
                return;

            var binding = (ContextBindingBase)target;
            var ctx = ResolveContext(binding, out var mode, out var ctxNameForLookup);

            EditorGUILayout.LabelField("Member Path", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // 현재 어떤 Context를 기준으로 드롭다운이 구성되는지 표시
            DrawContextInfo(ctx, mode, ctxNameForLookup);

            EditorGUILayout.PropertyField(_memberPathModeProp);

            var pathMode = (MemberPathMode)_memberPathModeProp.enumValueIndex;

            if (pathMode == MemberPathMode.Custom)
            {
                // 수동 입력 모드
                EditorGUILayout.PropertyField(_memberPathProp);
            }
            else
            {
                // Dropdown 모드
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.TextField("Path", _memberPathProp.stringValue);

                using (new EditorGUI.DisabledScope(ctx == null))
                {
                    if (GUILayout.Button("Select...", GUILayout.Width(70)))
                    {
                        ShowPathMenu(binding, ctx);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        void DrawContextInfo(DataContextBase ctx, ContextLookupMode mode, string ctxNameForLookup)
        {
            if (ctx != null)
            {
                // 예: Context: PlayerDataContext (ContextName="Player", Lookup=ByNameInParents)
                string label =
                    $"Context: {ctx.GetType().Name}  " +
                    $"(ContextName=\"{ctx.ContextName}\", Lookup={mode})";

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

        void ShowPathMenu(ContextBindingBase binding, DataContextBase ctx)
        {
            if (ctx == null)
            {
                EditorUtility.DisplayDialog("DataContext 없음",
                    "lookupMode 설정에 따라 DataContextBase 를 찾지 못했습니다.", "확인");
                return;
            }

            var vmType     = ctx.GetViewModelType();
            var vmInstance = ctx.GetDesignTimeViewModel();

            if (vmType == null)
            {
                EditorUtility.DisplayDialog("ViewModel 타입 없음",
                    "Design-time ViewModel 이나 런타임 ViewModel 타입을 알 수 없습니다.", "확인");
                return;
            }

            var candidates = new List<PathCandidate>();
            BuildCandidates(vmType, vmInstance, "", 0, candidates);

            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog("경로 없음",
                    "IObservableProperty / IObservableList / ICommand 타입 멤버를 찾지 못했습니다.", "확인");
                return;
            }

            var menu = new GenericMenu();
            foreach (var c in candidates)
            {
                string label    = c.DisplayLabel; // ex) "Health (ObservableProperty<int>)" 또는 "Stats/HP"
                bool   selected = _memberPathProp.stringValue == c.Path;

                menu.AddItem(new GUIContent(label), selected, () =>
                {
                    _memberPathProp.stringValue = c.Path;
                    serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        class PathCandidate
        {
            public string Path;         // 실제 memberPath 문자열
            public string DisplayLabel; // 메뉴에 보여줄 라벨
        }

        // ==============
        // Context Lookup (에디터용) – runtime과 동일한 정책을 사용
        // ==============

        DataContextBase ResolveContext(ContextBindingBase binding,
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
                    return binding.GetComponentInParent<DataContextBase>();

                case ContextLookupMode.ByNameInParents:
                    return FindContextInParentsByName(binding, ctxNameForLookup);

                case ContextLookupMode.ByNameInScene:
                    return FindContextInSceneByName(ctxNameForLookup);

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
        // Path 후보 빌드 (기존 코드)
        // ==============

        void BuildCandidates(Type type, object instance, string basePath, int depth, List<PathCandidate> acc)
        {
            if (depth > 4) // 너무 깊게 안 내려가도록
                return;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public;

            foreach (var p in type.GetProperties(flags))
            {
                if (p.GetIndexParameters().Length > 0)
                    continue;

                ProcessMember(p, p.PropertyType, mInstGetter: obj => SafeGet(() => p.GetValue(obj)),
                    type, instance, basePath, depth, acc);
            }

            foreach (var f in type.GetFields(flags))
            {
                ProcessMember(f, f.FieldType, mInstGetter: obj => SafeGet(() => f.GetValue(obj)),
                    type, instance, basePath, depth, acc);
            }
        }

        void ProcessMember(MemberInfo m, Type memberType,
            Func<object, object> mInstGetter,
            Type ownerType, object ownerInstance,
            string basePath, int depth,
            List<PathCandidate> acc)
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
                acc.Add(new PathCandidate
                {
                    Path         = currentPath,
                    DisplayLabel = label
                });
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
            if (typeof(IEnumerable).IsAssignableFrom(t) && t != typeof(string))
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
}
#endif
