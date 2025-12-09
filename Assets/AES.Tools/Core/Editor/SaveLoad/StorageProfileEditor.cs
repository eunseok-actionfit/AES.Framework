
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor.SaveLoad
{
    [CustomEditor(typeof(StorageProfile))]
    public sealed class StorageProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var profile = (StorageProfile)target;

            EditorGUILayout.HelpBox("SaveDataAttribute 로 등록된 타입들과 매칭되는 저장 설정을 관리합니다.", MessageType.Info);

            if (GUILayout.Button("SaveDataRegistry 에서 동기화"))
            {
                SyncFromRegistry(profile);
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();

            EditorGUILayout.Space();
            DrawValidation(profile);
        }

        void SyncFromRegistry(StorageProfile profile)
        {
            var existing = profile.entries.ToDictionary(e => e.id, e => e);
            var list = new List<StorageEntry>();

            foreach (var info in SaveDataRegistry.All)
            {
                if (!existing.TryGetValue(info.Id, out var entry))
                {
                    entry = new StorageEntry
                    {
                        id = info.Id,
                        useSlotOverride = null,
                        backendOverride = null
                    };
                }
                list.Add(entry);
            }

            profile.entries = list;
            EditorUtility.SetDirty(profile);
            Debug.Log("[StorageProfile] SaveDataRegistry와 동기화 완료");
        }

        void DrawValidation(StorageProfile profile)
        {
            var ids = new HashSet<string>();
            foreach (var e in profile.entries)
            {
                if (string.IsNullOrEmpty(e.id))
                {
                    EditorGUILayout.HelpBox("빈 id 항목이 있습니다.", MessageType.Warning);
                    continue;
                }

                if (!ids.Add(e.id))
                {
                    EditorGUILayout.HelpBox($"중복 id: {e.id}", MessageType.Error);
                }

                var inRegistry = SaveDataRegistry.All.Any(i => i.Id == e.id);
                if (!inRegistry)
                {
                    EditorGUILayout.HelpBox($"SaveDataRegistry 에 존재하지 않는 id: {e.id}", MessageType.Warning);
                }
            }
        }

        [MenuItem("AES/Save System/Create StorageProfile")]
        static void CreateProfile()
        {
            var asset = ScriptableObject.CreateInstance<StorageProfile>();
            const string path = "Assets/StorageProfile.asset";
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            Debug.Log($"[StorageProfile] 생성: {path}");
        }
    }
}
#endif