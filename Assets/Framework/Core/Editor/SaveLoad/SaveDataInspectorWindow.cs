#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AES.Tools.Core;
using AES.Tools.Impl;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;


public sealed class SaveDataInspectorWindow : EditorWindow
{
    [SerializeField] private StorageProfile profile;
    [SerializeField] private string slotId = "default";

    // 캐시된 데이터: id -> (SaveDataInfo, object 인스턴스)
    private readonly Dictionary<string, (SaveDataInfo info, object data)> _loaded
        = new Dictionary<string, (SaveDataInfo, object)>();

    private Vector2 _scroll;

    // 폴드아웃 상태 기억용
    private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

    // 런타임 파이프라인과 동일하게 쓰기 위해 직접 생성
    private ILocalBlobStore _local;
    private ICloudBlobStore _cloud;
    private IJsonSerializer _serializer;

    [MenuItem("Tools/Save System/Save Data Inspector")]
    public static void Open()
    {
        var win = GetWindow<SaveDataInspectorWindow>();
        win.titleContent = new GUIContent("Save Data Inspector");
        win.Show();
    }

    private void OnEnable()
    {
        // 프로젝트에 맞게 구현체 교체
        _local = new FileBlobStore();
        _cloud = new NullCloudBlobStore();
        _serializer = new JsonSerializer();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Save Data Inspector", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        profile = (StorageProfile)EditorGUILayout.ObjectField(
            "Storage Profile", profile, typeof(StorageProfile), false);

        slotId = EditorGUILayout.TextField("Slot Id", slotId);

        using (new EditorGUI.DisabledScope(profile == null))
        {
            if (GUILayout.Button("선택한 Profile 기준으로 저장 데이터 불러오기")) { LoadAll(); }
        }

        using (new EditorGUI.DisabledScope(_loaded.Count == 0))
        {
            if (GUILayout.Button("변경내용 모두 저장")) { SaveAll(); }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Loaded Entries", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (var kv in _loaded)
        {
            var id = kv.Key;
            var info = kv.Value.info;
            var data = kv.Value.data;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"ID: {id} (Type: {info.Type.Name})", EditorStyles.boldLabel);

            if (data == null) { EditorGUILayout.LabelField("데이터 없음 (null)"); }
            else { DrawObjectFields(data, info.Type); }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
    }

    void LoadAll()
    {
        LoadAllAsyncInternal().Forget();
    }

    async UniTask LoadAllAsyncInternal()
    {
        if (profile == null)
        {
            Debug.LogWarning("[SaveDataInspector] StorageProfile 이 지정되지 않았습니다.");
            return;
        }

        _loaded.Clear();
        var ct = CancellationToken.None;

        foreach (var entry in profile.entries)
        {
            if (string.IsNullOrEmpty(entry.id))
                continue;

            var info = SaveDataRegistry.All.FirstOrDefault(i => i.Id == entry.id);

            if (info == null)
            {
                Debug.LogWarning($"[SaveDataInspector] SaveDataRegistry 에 id='{entry.id}' 타입이 없습니다.");
                continue;
            }

            var useSlot = entry.useSlotOverride ?? info.UseSlot;
            var backend = entry.backendOverride ?? info.Backend;
            var key = useSlot ? $"{entry.id}_{slotId}" : entry.id;

            byte[] bytes = null;

            if (backend == SaveBackend.CloudFirst && _cloud != null)
            {
                try { bytes = await _cloud.LoadOrNullAsync(key, ct); }
                catch (Exception ex) { Debug.LogError($"[SaveDataInspector] Cloud load fail id={entry.id}, key={key}\n{ex}"); }
            }

            if (bytes == null)
            {
                try { bytes = await _local.LoadOrNullAsync(key, ct); }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveDataInspector] Local load fail id={entry.id}, key={key}\n{ex}");
                    continue;
                }
            }

            object dataObj = null;

            if (bytes != null)
            {
                try
                {
                    var jsonStr = Encoding.UTF8.GetString(bytes);
                    var method = typeof(IJsonSerializer)
                        .GetMethod(nameof(IJsonSerializer.Deserialize))
                        ?.MakeGenericMethod(info.Type);

                    if (method != null) dataObj = method.Invoke(_serializer, new object[] { jsonStr });
                }
                catch (Exception ex) { Debug.LogError($"[SaveDataInspector] Deserialize fail id={entry.id}\n{ex}"); }
            }

            if (dataObj == null)
            {
                try { dataObj = Activator.CreateInstance(info.Type); }
                catch (Exception ex) { Debug.LogError($"[SaveDataInspector] CreateInstance fail type={info.Type}\n{ex}"); }
            }

            _loaded[entry.id] = (info, dataObj);
        }

        Repaint();
    }

    void SaveAll()
    {
        SaveAllAsyncInternal().Forget();
    }

    async UniTask SaveAllAsyncInternal()
    {
        var ct = CancellationToken.None;

        foreach (var kv in _loaded)
        {
            var id = kv.Key;
            var info = kv.Value.info;
            var data = kv.Value.data;

            var entry = profile.Find(id);
            if (entry == null)
                continue;

            var useSlot = entry.useSlotOverride ?? info.UseSlot;
            var backend = entry.backendOverride ?? info.Backend;
            var key = useSlot ? $"{id}_{slotId}" : id;

            try
            {
                var methodToJson = typeof(IJsonSerializer)
                    .GetMethod(nameof(IJsonSerializer.Serialize))
                    ?.MakeGenericMethod(info.Type);

                if (methodToJson != null)
                {
                    var jsonStr = (string)methodToJson.Invoke(_serializer, new[] { data });
                    var bytes = Encoding.UTF8.GetBytes(jsonStr);

                    var rLocal = await _local.SaveAsync(key, bytes, ct);

                    if (rLocal.IsFail)
                    {
                        Debug.LogError($"[SaveDataInspector] Local save fail id={id}, key={key}, err={rLocal.Error}");
                        continue;
                    }

                    if (backend == SaveBackend.CloudFirst && _cloud != null)
                    {
                        var rCloud = await _cloud.SaveAsync(key, bytes, ct);

                        if (rCloud.IsFail) { Debug.LogError($"[SaveDataInspector] Cloud save fail id={id}, key={key}, err={rCloud.Error}"); }
                    }
                }

            }
            catch (Exception ex) { Debug.LogError($"[SaveDataInspector] Save serialize fail id={id}\n{ex}"); }
        }

        Debug.Log("[SaveDataInspector] 변경 내용 저장 완료");
    }

    // ------------------------
    // 타입 판별 유틸
    // ------------------------

    static bool IsSimpleType(Type t)
    {
        return t.IsPrimitive
               || t.IsEnum
               || t == typeof(string)
               || t == typeof(decimal);
    }

    static bool IsListType(Type t)
    {
        return typeof(System.Collections.IList).IsAssignableFrom(t);
    }

    static bool IsDictionaryType(Type t)
    {
        return typeof(System.Collections.IDictionary).IsAssignableFrom(t);
    }

    object CreateDefault(Type t)
    {
        if (t == typeof(string)) return "";
        if (t.IsValueType) return Activator.CreateInstance(t);

        try { return Activator.CreateInstance(t); }
        catch { return null; }
    }

    bool Foldout(string key, string label)
    {
        var state = _foldouts.GetValueOrDefault(key, false);

        var newState = EditorGUILayout.Foldout(state, label, true);
        if (newState != state)
            _foldouts[key] = newState;

        return newState;
    }

    // ------------------------
    // 렌더링
    // ------------------------

    void DrawObjectFields(object obj, Type type)
    {
        if (obj == null)
        {
            EditorGUILayout.LabelField("null");
            return;
        }

        EditorGUI.indentLevel++;

        var flags = System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic;

        foreach (var field in type.GetFields(flags))
        {
            if (!field.IsPublic &&
                field.GetCustomAttributes(typeof(SerializeField), true).Length == 0) { continue; }

            var fieldType = field.FieldType;
            var label = ObjectNames.NicifyVariableName(field.Name);
            var value = field.GetValue(obj);

            EditorGUI.BeginChangeCheck();
            object newValue = value;

            if (IsSimpleType(fieldType)) { newValue = DrawSimpleField(label, fieldType, value); }
            else if (IsListType(fieldType)) { newValue = DrawListField(label, fieldType, value); }
            else if (IsDictionaryType(fieldType)) { newValue = DrawDictionaryField(label, fieldType, value); }
            else if (fieldType.IsClass || (fieldType.IsValueType && !fieldType.IsPrimitive))
            {
                var foldoutKey = $"{type.FullName}.{field.Name}.{obj.GetHashCode()}";
                var header = $"{label} ({fieldType.Name})";

                var expanded = Foldout(foldoutKey, header);

                if (expanded)
                {
                    if (value == null)
                    {
                        try { value = Activator.CreateInstance(fieldType); }
                        catch
                        { // ignored
                        }
                    }

                    DrawObjectFields(value, fieldType);
                    newValue = value;
                }
            }
            else { EditorGUILayout.LabelField(label, value != null ? value.ToString() : "(null)"); }

            if (EditorGUI.EndChangeCheck()) { field.SetValue(obj, newValue); }
        }

        EditorGUI.indentLevel--;
    }

    object DrawSimpleField(string label, Type fieldType, object value)
    {
        if (fieldType == typeof(int))
            return EditorGUILayout.IntField(label, value != null ? (int)value : 0);

        if (fieldType == typeof(float))
            return EditorGUILayout.FloatField(label, value != null ? (float)value : 0f);

        if (fieldType == typeof(bool))
            return EditorGUILayout.Toggle(label, value != null && (bool)value);

        if (fieldType == typeof(string))
            return EditorGUILayout.TextField(label, value as string ?? "");

        if (fieldType.IsEnum)
            return EditorGUILayout.EnumPopup(label, (Enum)(value ?? Activator.CreateInstance(fieldType)));

        if (fieldType == typeof(decimal))
        {
            var dec = value != null ? (decimal)value : 0m;
            var dbl = (double)dec;
            dbl = EditorGUILayout.DoubleField(label, dbl);
            return (decimal)dbl;
        }

        EditorGUILayout.LabelField(label, value != null ? value.ToString() : "(null)");
        return value;
    }

    Type FindGenericListType(Type t)
    {
        while (t != null && t != typeof(object))
        {
            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                if (def == typeof(List<>) || def == typeof(IList<>))
                    return t;
            }

            t = t.BaseType;
        }

        return null;
    }

    object DrawListField(string label, Type listType, object value)
    {
        var list = value as System.Collections.IList;

        if (list == null)
        {
            try { list = (System.Collections.IList)Activator.CreateInstance(listType); }
            catch
            {
                EditorGUILayout.LabelField(label, "(리스트 생성 실패)");
                return value;
            }
        }

        Type elementType = null;

        if (listType.IsArray) { elementType = listType.GetElementType(); }
        else
        {
            var genericListType = FindGenericListType(listType);

            if (genericListType != null) { elementType = genericListType.GetGenericArguments()[0]; }
        }

        if (elementType == null)
        {
            EditorGUILayout.LabelField(label, "(비제네릭 IList, 편집 미지원)");
            return list;
        }

        bool isFixedSize = list.IsFixedSize;

        var foldoutKey = $"{listType.FullName}.{label}.{list.GetHashCode()}";


        var header = $"{label} (List<{elementType.Name}>) Size={list.Count}" +
                     (isFixedSize ? " [Fixed]" : "");

        var expanded = Foldout(foldoutKey, header);
        if (!expanded)
            return list;


        EditorGUI.indentLevel++;

        // 고정 크기 리스트(배열 등)는 Add/Remove 비활성화
        if (!isFixedSize)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+", GUILayout.Width(30)))
                list.Add(CreateDefault(elementType));

            if (GUILayout.Button("-", GUILayout.Width(30)) && list.Count > 0)
                list.RemoveAt(list.Count - 1);

            EditorGUILayout.EndHorizontal();
        }
        else { EditorGUILayout.HelpBox("고정 크기 컬렉션입니다. 요소 값만 수정 가능합니다.", MessageType.Info); }

        // 요소들 렌더링
        for (int i = 0; i < list.Count; i++)
        {
            var elem = list[i];
            var elemLabel = $"[{i}]";

            EditorGUI.BeginChangeCheck();

            if (IsSimpleType(elementType))
            {
                var newElem = DrawSimpleField(elemLabel, elementType, elem);
                if (EditorGUI.EndChangeCheck())
                    list[i] = newElem;
            }
            else
            {
                EditorGUILayout.LabelField(elemLabel, EditorStyles.boldLabel);

                if (elem == null)
                {
                    elem = CreateDefault(elementType);
                    list[i] = elem;
                }

                DrawObjectFields(elem, elementType);
                EditorGUI.EndChangeCheck();
            }
        }

        EditorGUI.indentLevel--;

        return list;
    }

    Type FindGenericDictionaryType(Type t)
    {
        while (t != null && t != typeof(object))
        {
            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                if (def == typeof(Dictionary<,>))
                    return t;
            }

            t = t.BaseType;
        }

        return null;
    }


    object DrawDictionaryField(string label, Type dictType, object value)
    {
        var dict = value as System.Collections.IDictionary;

        if (dict == null)
        {
            try { dict = (System.Collections.IDictionary)Activator.CreateInstance(dictType); }
            catch
            {
                EditorGUILayout.LabelField(label, "(Dictionary 생성 실패)");
                return value;
            }
        }

        // ★ 핵심: 제네릭 딕셔너리 타입 찾기
        var genericDicType = FindGenericDictionaryType(dictType);

        if (genericDicType == null)
        {
            EditorGUILayout.LabelField(label, "(비제네릭 IDictionary, 편집 제한)");
            return dict;
        }

        var args = genericDicType.GetGenericArguments();
        var keyType = args[0];
        var valueType = args[1];

        var foldoutKey = $"{dictType.FullName}.{label}.{dict.GetHashCode()}";
        var header = $"{label} (Dictionary<{keyType.Name}, {valueType.Name}>) Count={dict.Count}";

        var expanded = Foldout(foldoutKey, header);
        if (!expanded)
            return dict;

        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField($"Count: {dict.Count}");
        EditorGUILayout.HelpBox("Dictionary 구조는 고정입니다. 값만 수정 가능합니다.", MessageType.Info);

        // 키/값 렌더링 (키는 읽기 전용, 값만 편집)
        foreach (System.Collections.DictionaryEntry entry in dict)
        {
            EditorGUILayout.BeginVertical("box");

            var k = entry.Key;
            var v = entry.Value;

            EditorGUILayout.LabelField($"Key: {k}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            if (IsSimpleType(valueType))
            {
                var newVal = DrawSimpleField("Value", valueType, v);
                if (EditorGUI.EndChangeCheck())
                    dict[k] = newVal;
            }
            else
            {
                if (v == null)
                {
                    v = CreateDefault(valueType);
                    dict[k] = v;
                }

                DrawObjectFields(v, valueType);
                EditorGUI.EndChangeCheck();
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUI.indentLevel--;

        return dict;
    }


}
#endif