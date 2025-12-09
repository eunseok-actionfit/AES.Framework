#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using AES.Tools.Core;
using AES.Tools.Impl;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;


namespace AES.Tools.Editor.SaveLoad
{
    public sealed class SaveDataInspectorWindow : EditorWindow
    {
        [SerializeField] private StorageProfile profile;
        [SerializeField] private string slotId = "default";
        private const string EDITOR_PREF_KEY_AUTO_DELETE_ON_PLAY = "SaveDataInspector_AutoDeleteOnPlay";
        private bool _autoDeleteOnPlay;

        // 캐시된 데이터: id -> (SaveDataInfo, object 인스턴스)
        private readonly Dictionary<string, (SaveDataInfo info, object data)> _loaded = new();

        private Vector2 _scroll;

        // 폴드아웃 상태 기억용
        private readonly Dictionary<string, bool> _foldouts = new();

        // 런타임 파이프라인과 동일하게 쓰기 위해 직접 생성
        private ILocalBlobStore _local;
        private ICloudBlobStore _cloud;
        private IJsonSerializer _serializer;

        [MenuItem("AES/Save System/Save Data Inspector")]
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
            _serializer = new NewtonsoftJsonSerializer();

            _autoDeleteOnPlay = EditorPrefs.GetBool(EDITOR_PREF_KEY_AUTO_DELETE_ON_PLAY, false);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Save Data Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            profile = (StorageProfile)EditorGUILayout.ObjectField(
                "Storage Profile", profile, typeof(StorageProfile), false);

            slotId = EditorGUILayout.TextField("Slot Id", slotId);

            // 에디터 플레이 시작 시 자동 삭제 옵션
            var newAutoDeleteOnPlay = EditorGUILayout.ToggleLeft(
                "에디터 플레이 시작 시 현재 Profile 저장 데이터 자동 삭제",
                _autoDeleteOnPlay);

            if (newAutoDeleteOnPlay != _autoDeleteOnPlay)
            {
                _autoDeleteOnPlay = newAutoDeleteOnPlay;
                EditorPrefs.SetBool(EDITOR_PREF_KEY_AUTO_DELETE_ON_PLAY, _autoDeleteOnPlay);
            }

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
                    Debug.LogWarning($"[SaveDataInspector] SaveDataInfo 를 찾을 수 없습니다. id={entry.id}");
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
                    catch (Exception ex) { Debug.LogError($"[SaveDataInspector] Local load fail id={entry.id}, key={key}\n{ex}"); }
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
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveDataInspector] CreateInstance fail id={entry.id}, type={info.Type}\n{ex}");
                        continue;
                    }
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

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!_autoDeleteOnPlay || profile == null)
                return;

            // 에디터에서 플레이 버튼을 눌러 EditMode 를 빠져나갈 때 한 번 실행
            if (state == PlayModeStateChange.ExitingEditMode) { DeleteAllSavesForProfile().Forget(); }
        }

        // ILocalBlobStore / ICloudBlobStore 에 DeleteAsync(key, CancellationToken) 메서드가 있어야 합니다.
        async UniTask DeleteAllSavesForProfile()
        {
            if (profile == null)
                return;

            var ct = CancellationToken.None;

            foreach (var entry in profile.entries)
            {
                if (string.IsNullOrEmpty(entry.id))
                    continue;

                var info = SaveDataRegistry.All.FirstOrDefault(i => i.Id == entry.id);
                if (info == null)
                    continue;

                var useSlot = entry.useSlotOverride ?? info.UseSlot;
                var backend = entry.backendOverride ?? info.Backend;
                var key = useSlot ? $"{entry.id}_{slotId}" : entry.id;

                try
                {
                    var rLocal = await _local.DeleteAsync(key, ct);

                    if (rLocal.IsFail) { Debug.LogError($"[SaveDataInspector] Local delete fail id={entry.id}, key={key}, err={rLocal.Error}"); }
                }
                catch (Exception ex) { Debug.LogError($"[SaveDataInspector] Local delete exception id={entry.id}, key={key}\n{ex}"); }

                if (backend == SaveBackend.CloudFirst && _cloud != null)
                {
                    try
                    {
                        var rCloud = await _cloud.DeleteAsync(key, ct);

                        if (rCloud.IsFail) { Debug.LogError($"[SaveDataInspector] Cloud delete fail id={entry.id}, key={key}, err={rCloud.Error}"); }
                    }
                    catch (Exception ex) { Debug.LogError($"[SaveDataInspector] Cloud delete exception id={entry.id}, key={key}\n{ex}"); }
                }
            }

            _loaded.Clear();
            Debug.Log("[SaveDataInspector] 플레이 시작 전 자동 삭제 완료");
        }

        // ------------------------
        // 타입 판별 유틸
        // ------------------------
        static bool IsSimpleType(Type t)
        {
            return t.IsPrimitive
                   || t.IsEnum
                   || t == typeof(string)
                   || t == typeof(decimal)
                   || t == typeof(BigInteger)
                   || t == typeof(DateTime); 
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

        // ------------------------
        // 오브젝트 필드 그리기
        // ------------------------

        void DrawObjectFields(object obj, Type type)
        {
            if (obj == null)
            {
                EditorGUILayout.LabelField("(null)");
                return;
            }

            if (IsSimpleType(type))
            {
                EditorGUILayout.LabelField("값 타입은 최상위에서 직접 수정 불가합니다.");
                return;
            }

            EditorGUI.indentLevel++;

            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                var fieldType = field.FieldType;
                var fieldValue = field.GetValue(obj);
                var label = ObjectNames.NicifyVariableName(field.Name);

                EditorGUI.BeginChangeCheck();

                if (IsSimpleType(fieldType))
                {
                    var newValue = DrawSimpleField(label, fieldType, fieldValue);

                    if (EditorGUI.EndChangeCheck()) { field.SetValue(obj, newValue); }
                }
                else if (IsListType(fieldType))
                {
                    var newValue = DrawListField(label, fieldType, fieldValue);

                    if (EditorGUI.EndChangeCheck()) { field.SetValue(obj, newValue); }
                }
                else if (IsDictionaryType(fieldType))
                {
                    var newValue = DrawDictionaryField(label, fieldType, fieldValue);

                    if (EditorGUI.EndChangeCheck()) { field.SetValue(obj, newValue); }
                }
                else
                {
                    if (fieldValue == null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(label + " (null reference)");

                        if (GUILayout.Button("Create", GUILayout.Width(80))) { fieldValue = CreateDefault(fieldType); }

                        EditorGUILayout.EndHorizontal();

                        if (fieldValue == null) continue;
                    }

                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                    DrawObjectFields(fieldValue, fieldType);
                    EditorGUI.EndChangeCheck();
                }
            }

            EditorGUI.indentLevel--;
        }

        object DrawSimpleField(string label, Type fieldType, object value)
        {
            // DateTime: 읽기 전용 표시 + 버튼으로만 수정
            if (fieldType == typeof(DateTime))
            {
                // 원래 값
                DateTime original = value != null ? (DateTime)value : default;
                DateTime dt = original;

                EditorGUILayout.BeginVertical("box");

                // 표시용 ISO 문자열
                string iso;
                if (dt == default)
                    iso = "(default)";
                else
                    iso = dt.ToString("yyyy-MM-dd HH:mm:ss");

                // 메인 라벨 + ISO
                EditorGUILayout.LabelField(label, iso);

                // Relative 표시 (읽기 전용)
                if (dt != default)
                {
                    string relative = GetRelativeText(dt);
                    EditorGUILayout.LabelField("Relative", relative);
                }

                // 수정은 버튼으로만
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Now")) dt = DateTime.Now;
                if (GUILayout.Button("+1h")) dt = dt.AddHours(1);
                if (GUILayout.Button("+1d")) dt = dt.AddDays(1);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                if (dt != original)
                    GUI.changed = true;

                return dt;
            }

            // int
            if (fieldType == typeof(int))
                return EditorGUILayout.IntField(label, value != null ? (int)value : 0);

            // long
            if (fieldType == typeof(long))
                return EditorGUILayout.LongField(label, value != null ? (long)value : 0L);

            // ulong (ULongField가 없어서 TextField 기반으로 처리)
            if (fieldType == typeof(ulong))
            {
                ulong current = value != null ? (ulong)value : 0UL;
                string str = current.ToString();
                string newStr = EditorGUILayout.TextField(label, str);

                if (newStr != str && ulong.TryParse(newStr, out var parsed))
                    return parsed;

                return current;
            }

            // float + compact 표시
            if (fieldType == typeof(float))
            {
                float f = value != null ? (float)value : 0f;

                EditorGUILayout.BeginHorizontal();
                f = EditorGUILayout.FloatField(label, f);

                // compact 텍스트 (예: 12345 -> 12.3k)
                EditorGUILayout.LabelField(f.ToCompact(), GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                return f;
            }

            // double + compact 표시
            if (fieldType == typeof(double))
            {
                double d = value != null ? (double)value : 0d;

                EditorGUILayout.BeginHorizontal();
                d = EditorGUILayout.DoubleField(label, d);

                EditorGUILayout.LabelField(d.ToCompact(), GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                return d;
            }

            // bool
            if (fieldType == typeof(bool))
                return EditorGUILayout.Toggle(label, value != null && (bool)value);

            // string
            if (fieldType == typeof(string))
                return EditorGUILayout.TextField(label, value as string ?? "");

            // enum
            if (fieldType.IsEnum)
                return EditorGUILayout.EnumPopup(label, (Enum)(value ?? Activator.CreateInstance(fieldType)));

            // decimal + compact 표시 (원하면)
            if (fieldType == typeof(decimal))
            {
                var dec = value != null ? (decimal)value : 0m;
                double dbl = (double)dec;

                EditorGUILayout.BeginHorizontal();
                dbl = EditorGUILayout.DoubleField(label, dbl);

                // decimal은 double로 변환해서 compact 표시
                EditorGUILayout.LabelField(dbl.ToCompact(), GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();

                return (decimal)dbl;
            }

            // BigInteger (문자열로 편집)
            if (fieldType == typeof(BigInteger))
            {
                var bi = value is BigInteger b ? b : BigInteger.Zero;
                string str = bi.ToString();

                EditorGUILayout.BeginHorizontal();
                string newStr = EditorGUILayout.TextField(label, str);

                // 크면 compact 한 줄 더 보여주는 것도 가능
                string compact = BigIntegerToCompactSafe(bi);
                if (!string.IsNullOrEmpty(compact))
                    EditorGUILayout.LabelField(compact, GUILayout.Width(80));

                EditorGUILayout.EndHorizontal();

                if (newStr != str && BigInteger.TryParse(newStr, out var parsed))
                    return parsed;

                return bi;
            }

            EditorGUILayout.LabelField(label, $"(지원되지 않는 단순 타입: {fieldType.Name})");
            return value;
        }
    
        string GetRelativeText(DateTime dt)
        {
            var diff = DateTime.Now - dt;

            if (diff.TotalSeconds < 1) return "지금";
            if (diff.TotalMinutes < 1) return $"{diff.Seconds}초 전";
            if (diff.TotalHours < 1) return $"{diff.Minutes}분 전";
            if (diff.TotalDays < 1) return $"{diff.Hours}시간 전";
            if (diff.TotalDays < 7) return $"{diff.Days}일 전";

            return dt.ToString("yyyy-MM-dd");
        }

        string BigIntegerToCompactSafe(BigInteger bi)
        {
            // double로 안전하게 표현되는 범위 내에서만
            if (bi <= long.MaxValue && bi >= long.MinValue)
            {
                long l = (long)bi;
                return l.ToCompact(); // long 확장 메서드 사용
            }

            // 너무 크면 대략적인 자릿수만
            int digits = bi.ToString().Length;
            return $"{digits} digits";
        }


        // ------------------------
        // 리스트 렌더링
        // ------------------------

        object DrawListField(string label, Type listType, object value)
        {
            if (value == null)
            {
                try { value = Activator.CreateInstance(listType); }
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
                EditorGUILayout.LabelField(label, "(알 수 없는 리스트 요소 타입)");
                return value;
            }

            var list = value as System.Collections.IList;

            if (list == null)
            {
                EditorGUILayout.LabelField(label, "(IList 아님)");
                return value;
            }

            var foldoutKey = $"{listType.FullName}.{label}.{value.GetHashCode()}";
            var header = $"{label} (List<{elementType.Name}>) Count={list.Count}";

            var expanded = Foldout(foldoutKey, header);

            if (!expanded)
                return value;

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField($"Count: {list.Count}");

            if (!listType.IsArray)
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
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("(null)");

                        if (GUILayout.Button("Create", GUILayout.Width(80)))
                        {
                            elem = CreateDefault(elementType);
                            list[i] = elem;
                        }

                        EditorGUILayout.EndHorizontal();

                        if (elem == null) continue;
                    }

                    DrawObjectFields(elem, elementType);
                    EditorGUI.EndChangeCheck();
                }
            }

            EditorGUI.indentLevel--;

            return value;
        }

        static Type FindGenericListType(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                return t;

            var interfaces = t.GetInterfaces();

            foreach (var i in interfaces)
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(List<>))
                    return i;
            }

            return null;
        }

        // ------------------------
        // Dictionary 렌더링
        // ------------------------

        object DrawDictionaryField(string label, Type dictType, object value)
        {
            if (value == null)
            {
                try { value = Activator.CreateInstance(dictType); }
                catch
                {
                    EditorGUILayout.LabelField(label, "(Dictionary 생성 실패)");
                    return value;
                }
            }

            var dict = value as System.Collections.IDictionary;

            if (dict == null)
            {
                EditorGUILayout.LabelField(label, "(IDictionary 아님)");
                return value;
            }

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

                EditorGUILayout.LabelField("Key", k != null ? k.ToString() : "(null)");

                EditorGUI.BeginChangeCheck();

                if (IsSimpleType(valueType))
                {
                    var newValue = DrawSimpleField("Value", valueType, v);
                    if (EditorGUI.EndChangeCheck())
                        if (k != null)
                            dict[k] = newValue;
                }
                else
                {
                    if (v == null)
                    {
                        v = CreateDefault(valueType);
                        if (k != null) dict[k] = v;
                    }

                    DrawObjectFields(v, valueType);
                    EditorGUI.EndChangeCheck();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;

            return dict;
        }

        static Type FindGenericDictionaryType(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return t;

            var interfaces = t.GetInterfaces();

            foreach (var i in interfaces)
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    return i;
            }

            return null;
        }

        // ------------------------
        // Foldout 상태 저장 유틸
        // ------------------------

        bool Foldout(string key, string label)
        {
            var expanded = _foldouts.GetValueOrDefault(key, false);

            expanded = EditorGUILayout.Foldout(expanded, label, true);
            _foldouts[key] = expanded;
            return expanded;
        }
    }
}
#endif