// ContextListenerBaseEditor.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor
{
    [CustomEditor(typeof(ContextListenerBase), true)]
    public class ContextListenerBaseEditor : UnityEditor.Editor
    {
        SerializedProperty _lookupModeProp;
        SerializedProperty _contextNameProp;

        void OnEnable()
        {
            if (target == null) return;

            _lookupModeProp  = serializedObject.FindProperty("lookupMode");
            _contextNameProp = serializedObject.FindProperty("contextName");
        }

        public override void OnInspectorGUI()
        {
            if (target == null) return;

            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject,
                "lookupMode",
                "contextName");

            EditorGUILayout.Space();
            DrawContextSection();
            EditorGUILayout.Space();
            DrawViewModelBrowserSection();

            serializedObject.ApplyModifiedProperties();
        }

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

            var binding = (ContextListenerBase)target;
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
                    string logicalName =
                        mb is MonoContext dc ? dc.ContextName : mb.gameObject.name;
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
                    "ContextName 은 수동 입력으로도 사용할 수 있습니다.",
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

        void DrawViewModelBrowserSection()
        {
            EditorGUILayout.LabelField("ViewModel Browser (Read-only)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var binding = (ContextListenerBase)target;
            var provider = ResolveProvider(binding, out var mode, out var ctxNameForLookup);

            if (provider is MonoBehaviour mb)
            {
                string ctxName =
                    mb is MonoContext dc ? dc.ContextName : mb.gameObject.name;

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
                EditorGUI.indentLevel--;
                return;
            }

            using (new EditorGUI.DisabledScope(provider == null))
            {
                if (GUILayout.Button("Browse Members..."))
                    ShowPathMenu(provider);
            }

            EditorGUI.indentLevel--;
        }

        IBindingContextProvider ResolveProvider(ContextListenerBase binding,
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

        IBindingContextProvider FindProviderInParentsByName(ContextListenerBase binding, string name)
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

        // 여기서 ViewModel 멤버 브라우저는 필요 최소한으로: path를 클립보드에 복사만.
        void ShowPathMenu(IBindingContextProvider provider)
        {
            if (provider == null)
            {
                EditorUtility.DisplayDialog("Context 없음",
                    "lookupMode 설정에 따라 IBindingContextProvider 를 찾지 못했습니다.", "확인");
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

            var candidates = new List<(string path, string label)>();
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
                menu.AddItem(new GUIContent(c.label), false, () =>
                {
                    EditorGUIUtility.systemCopyBuffer = c.path;
                    Debug.Log($"[ContextListenerBaseEditor] Copied path: {c.path}");
                });
            }
            menu.ShowAsContext();
        }

        // 아래 BuildCandidates는 간단 버전 (상세한 표시가 필요하면 기존 Editor 코드에서 확장)
        void BuildCandidates(Type rootType, object instance, string basePath, int depth,
            List<(string path, string label)> result)
        {
            if (depth > 5 || rootType == null)
                return;

            var flags = System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public   |
                        System.Reflection.BindingFlags.NonPublic;

            foreach (var prop in rootType.GetProperties(flags))
            {
                var memPath = string.IsNullOrEmpty(basePath)
                    ? prop.Name
                    : basePath + "." + prop.Name;

                // 단순히 경로/타입 정도만 브라우즈
                string label = $"{memPath} : {prop.PropertyType.Name}";

                result.Add((memPath, label));
            }

            foreach (var field in rootType.GetFields(flags))
            {
                var memPath = string.IsNullOrEmpty(basePath)
                    ? field.Name
                    : basePath + "." + field.Name;

                string label = $"{memPath} : {field.FieldType.Name}";
                result.Add((memPath, label));
            }
        }
    }
}
#endif
