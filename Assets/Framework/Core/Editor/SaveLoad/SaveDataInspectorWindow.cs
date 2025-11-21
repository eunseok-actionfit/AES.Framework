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


namespace AES.Tools.Editor
{
    public sealed class SaveDataInspectorWindow : EditorWindow
    {
        [SerializeField] private StorageProfile _profile;
        [SerializeField] private string _slotId = "default";

        // 캐시된 데이터: id -> (Type, object)
        private readonly Dictionary<string, (SaveDataInfo info, object data)> _loaded
            = new Dictionary<string, (SaveDataInfo, object)>();

        private Vector2 _scroll;

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
            _local = new FileBlobStore();
            _cloud = new NullCloudBlobStore();
            _serializer = new NewtonsoftJsonSerializer();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Save Data Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _profile = (StorageProfile)EditorGUILayout.ObjectField(
                "Storage Profile", _profile, typeof(StorageProfile), false);

            _slotId = EditorGUILayout.TextField("Slot Id", _slotId);

            using (new EditorGUI.DisabledScope(_profile == null))
            {
                if (GUILayout.Button("선택한 Profile 기준으로 저장 데이터 불러오기"))
                {
                    _ = LoadAllAsync(); // fire-and-forget
                }
            }

            using (new EditorGUI.DisabledScope(_loaded.Count == 0))
            {
                if (GUILayout.Button("변경내용 모두 저장")) { _ = SaveAllAsync(); }
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

        async UniTaskVoid LoadAllAsync()
        {
            if (_profile == null)
            {
                Debug.LogWarning("[SaveDataInspector] StorageProfile 이 지정되지 않았습니다.");
                return;
            }

            _loaded.Clear();

            var ct = CancellationToken.None;

            foreach (var entry in _profile.entries)
            {
                if (string.IsNullOrEmpty(entry.id))
                    continue;

                var info = SaveDataRegistry.All.FirstOrDefault(i => i.Id == entry.id);

                if (info == null)
                {
                    Debug.LogWarning($"[SaveDataInspector] SaveDataRegistry 에 id='{entry.id}' 타입이 없습니다.");
                    continue;
                }

                // StorageService의 EffectiveUseSlot / EffectiveBackend와 동일한 규칙 사용
                var useSlot = entry.useSlotOverride ?? info.UseSlot;
                var backend = entry.backendOverride ?? info.Backend;

                var key = useSlot ? $"{entry.id}_{_slotId}" : entry.id;

                byte[] bytes = null;

                // CloudFirst
                if (backend == SaveBackend.CloudFirst && _cloud != null)
                {
                    try { bytes = await _cloud.LoadOrNullAsync(key, ct); }
                    catch (Exception ex) { Debug.LogError($"[SaveDataInspector] Cloud load fail id={entry.id}, key={key}\n{ex}"); }
                }

                // Local fallback / LocalOnly
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

                // 데이터가 없으면 새 인스턴스라도 만든다 (편집해서 저장 가능)
                if (dataObj == null)
                {
                    try { dataObj = Activator.CreateInstance(info.Type); }
                    catch (Exception ex) { Debug.LogError($"[SaveDataInspector] CreateInstance fail type={info.Type}\n{ex}"); }
                }

                _loaded[entry.id] = (info, dataObj);
            }

            Repaint();
        }

        async UniTaskVoid SaveAllAsync()
        {
            var ct = CancellationToken.None;

            foreach (var kv in _loaded)
            {
                var id = kv.Key;
                var info = kv.Value.info;
                var data = kv.Value.data;

                var entry = _profile.Find(id);
                if (entry == null)
                    continue;

                var useSlot = entry.useSlotOverride ?? info.UseSlot;
                var backend = entry.backendOverride ?? info.Backend;

                var key = useSlot ? $"{id}_{_slotId}" : id;

                try
                {
                    // object -> json
                    var methodToJson = typeof(IJsonSerializer)
                        .GetMethod(nameof(IJsonSerializer.Serialize))
                        ?.MakeGenericMethod(info.Type);

                    if (methodToJson != null)
                    {
                        var jsonStr = (string)methodToJson.Invoke(_serializer, new[] { data });
                        var bytes = Encoding.UTF8.GetBytes(jsonStr);

                        // 항상 Local 저장
                        var rLocal = await _local.SaveAsync(key, bytes, ct);

                        if (rLocal.IsFail)
                        {
                            Debug.LogError($"[SaveDataInspector] Local save fail id={id}, key={key}, err={rLocal.Error}");
                            continue;
                        }

                        // CloudFirst면 Cloud에도
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

        void DrawObjectFields(object obj, Type type)
        {
            if (obj == null)
            {
                EditorGUILayout.LabelField("null");
                return;
            }

            EditorGUI.indentLevel++;

            var flags = System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic;

            foreach (var field in type.GetFields(flags))
            {
                // private [SerializeField] 이거나 public 필드만 편집 대상으로
                if (!field.IsPublic &&
                    field.GetCustomAttributes(typeof(SerializeField), true).Length == 0) { continue; }

                var fieldType = field.FieldType;
                var label = ObjectNames.NicifyVariableName(field.Name);
                var value = field.GetValue(obj);

                EditorGUI.BeginChangeCheck();

                object newValue = value;

                // 기본 타입 처리
                if (IsSimpleType(fieldType)) { newValue = DrawSimpleField(label, fieldType, value); }
                else if (IsListType(fieldType)) { newValue = DrawListField(label, fieldType, value); }
                else if (IsDictionaryType(fieldType)) { newValue = DrawDictionaryField(label, fieldType, value); }
                else if (fieldType.IsClass || (fieldType.IsValueType && !fieldType.IsPrimitive))
                {
                    // 중첩 복합 타입
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                    if (value == null)
                    {
                        try { value = Activator.CreateInstance(fieldType); }
                        catch { }
                    }

                    DrawObjectFields(value, fieldType);
                    newValue = value;
                }
                else
                {
                    // 지원 안 하는 타입은 일단 읽기 전용
                    EditorGUILayout.LabelField(label, value != null ? value.ToString() : "(null)");
                }

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


            // fallback
            EditorGUILayout.LabelField(label, value != null ? value.ToString() : "(null)");
            return value;
        }

        object DrawListField(string label, Type listType, object value)
        {
            var list = value as System.Collections.IList;

            // null이면 새 인스턴스 생성 시도
            if (list == null)
            {
                try { list = (System.Collections.IList)Activator.CreateInstance(listType); }
                catch
                {
                    EditorGUILayout.LabelField(label, "(리스트 생성 실패)");
                    return value;
                }
            }

            // 요소 타입 추출 (List<T> 또는 배열 등)
            Type elementType = null;

            if (listType.IsArray) { elementType = listType.GetElementType(); }
            else if (listType.IsGenericType) { elementType = listType.GetGenericArguments()[0]; }
            else
            {
                // 비제네릭 IList는 타입 알기 애매하니 string 정도로 가정하거나 읽기만
                EditorGUILayout.LabelField(label, "(비제네릭 IList, 편집 미지원)");
                return list;
            }

            var foldoutKey = label + "_" + list.GetHashCode();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            int count = list.Count;
            EditorGUILayout.LabelField($"Size: {count}");

            // Add/Remove 버튼
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+", GUILayout.Width(30))) { list.Add(CreateDefault(elementType)); }

            if (GUILayout.Button("-", GUILayout.Width(30)) && list.Count > 0) { list.RemoveAt(list.Count - 1); }

            EditorGUILayout.EndHorizontal();

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

        object CreateDefault(Type t)
        {
            if (t == typeof(string)) return "";
            if (t.IsValueType) return Activator.CreateInstance(t);

            try { return Activator.CreateInstance(t); }
            catch { return null; }
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

            Type keyType = null;
            Type valueType = null;

            if (dictType.IsGenericType)
            {
                var args = dictType.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];
            }
            else
            {
                EditorGUILayout.LabelField(label, "(비제네릭 IDictionary, 편집 제한)");
                return dict;
            }

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField($"Count: {dict.Count}");

            // 새 엔트리 추가용 입력 필드 (간단하게 string/int 키만 지원)
            object newKey = null;

            if (keyType == typeof(string))
            {
                var keyStr = EditorGUILayout.TextField("New Key (string)", "");

                if (!string.IsNullOrEmpty(keyStr) && GUILayout.Button("Add Entry")) { newKey = keyStr; }
            }
            else if (keyType == typeof(int))
            {
                int keyInt = 0;
                keyInt = EditorGUILayout.IntField("New Key (int)", keyInt);

                if (GUILayout.Button("Add Entry")) { newKey = keyInt; }
            }
            else { EditorGUILayout.HelpBox("Dictionary 키 타입이 string/int 가 아니므로 Add UI는 생략됩니다.", MessageType.Info); }

            if (newKey != null && !dict.Contains(newKey)) { dict[newKey] = CreateDefault(valueType); }

            // 기존 엔트리 렌더링
            var removeKeys = new List<object>();

            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                EditorGUILayout.BeginVertical("box");

                var k = entry.Key;
                var v = entry.Value;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Key: {k}", EditorStyles.boldLabel);

                if (GUILayout.Button("X", GUILayout.Width(20))) { removeKeys.Add(k); }

                EditorGUILayout.EndHorizontal();

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

            // 삭제 버튼 누른 항목 제거
            foreach (var k in removeKeys)
                dict.Remove(k);

            EditorGUI.indentLevel--;

            return dict;
        }





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

    }
}
#endif