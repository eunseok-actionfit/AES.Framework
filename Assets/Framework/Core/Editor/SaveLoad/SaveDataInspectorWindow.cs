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
                if (GUILayout.Button("변경내용 모두 저장"))
                {
                    _ = SaveAllAsync();
                }
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

                if (data == null)
                {
                    EditorGUILayout.LabelField("데이터 없음 (null)");
                }
                else
                {
                    DrawObjectFields(data, info.Type);
                }

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
                    try
                    {
                        bytes = await _cloud.LoadOrNullAsync(key, ct);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveDataInspector] Cloud load fail id={entry.id}, key={key}\n{ex}");
                    }
                }

                // Local fallback / LocalOnly
                if (bytes == null)
                {
                    try
                    {
                        bytes = await _local.LoadOrNullAsync(key, ct);
                    }
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
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveDataInspector] Deserialize fail id={entry.id}\n{ex}");
                    }
                }

                // 데이터가 없으면 새 인스턴스라도 만든다 (편집해서 저장 가능)
                if (dataObj == null)
                {
                    try
                    {
                        dataObj = Activator.CreateInstance(info.Type);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SaveDataInspector] CreateInstance fail type={info.Type}\n{ex}");
                    }
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
                            if (rCloud.IsFail)
                            {
                                Debug.LogError($"[SaveDataInspector] Cloud save fail id={id}, key={key}, err={rCloud.Error}");
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveDataInspector] Save serialize fail id={id}\n{ex}");
                }
            }

            Debug.Log("[SaveDataInspector] 변경 내용 저장 완료");
        }

        // 아주 단순한 Reflection 기반 필드 에디터
        // (필요하면 List/Dictionary 등 확장)
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
                // private [SerializeField] 이거나 public 필드만 편집 대상으로 본다
                if (!field.IsPublic &&
                    field.GetCustomAttributes(typeof(SerializeField), true).Length == 0)
                {
                    continue;
                }

                var fieldType = field.FieldType;
                var label = ObjectNames.NicifyVariableName(field.Name);
                var value = field.GetValue(obj);

                EditorGUI.BeginChangeCheck();

                object newValue = value;

                if (fieldType == typeof(int))
                    newValue = EditorGUILayout.IntField(label, value != null ? (int)value : 0);
                else if (fieldType == typeof(float))
                    newValue = EditorGUILayout.FloatField(label, value != null ? (float)value : 0f);
                else if (fieldType == typeof(bool))
                    newValue = EditorGUILayout.Toggle(label, value != null && (bool)value);
                else if (fieldType == typeof(string))
                    newValue = EditorGUILayout.TextField(label, value as string ?? "");
                else if (fieldType.IsEnum)
                    newValue = EditorGUILayout.EnumPopup(label, (Enum)(value ?? Activator.CreateInstance(fieldType)));
                else if (fieldType.IsClass || (fieldType.IsValueType && !fieldType.IsPrimitive))
                {
                    // 중첩 타입은 폴드아웃 형식으로 재귀
                    EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
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
                else
                {
                    // 지원 안 하는 타입은 텍스트로만 표시
                    EditorGUILayout.LabelField(label, value != null ? value.ToString() : "(null)");
                }

                if (EditorGUI.EndChangeCheck())
                {
                    field.SetValue(obj, newValue);
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
#endif
