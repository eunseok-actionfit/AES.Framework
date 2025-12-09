// SharedDrawer.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace AES.Tools.Editor.DataBinding
{
    [CustomPropertyDrawer(typeof(Shared<>))]
    public sealed class SharedDrawer : PropertyDrawer
    {
        private const float SourceToggleWidth = 16f;
        private const float ButtonWidth       = 22f;

        // groupId 인라인 UI용
        private const float GroupFieldWidth   = 80f;
        private const float GroupPopupWidth   = 18f;

        private const string RecentKey        = "AES.SharedDrawer.RecentGroupIds";
        private static List<string> _recentGroupIds;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureRecentLoaded();

            var valueProp    = property.FindPropertyRelative("value");
            var isSourceProp = property.FindPropertyRelative("isSource");
            var scopeProp    = property.FindPropertyRelative("scope");
            var groupProp    = property.FindPropertyRelative("groupId");

            if (valueProp == null || isSourceProp == null || scopeProp == null || groupProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Broken Shared<T>");
                return;
            }

            // 레이아웃: [Toggle][Value][GroupId][▼][...]
            var toggleRect = new Rect(position.x, position.y, SourceToggleWidth, position.height);

            float x = position.x + SourceToggleWidth + 2f;
            float w = position.width - SourceToggleWidth - 2f;

            var valueRect  = new Rect(x,
                                      position.y,
                                      w - (GroupFieldWidth + GroupPopupWidth + ButtonWidth + 4f),
                                      position.height);

            var groupRect  = new Rect(valueRect.xMax + 2f,
                                      position.y,
                                      GroupFieldWidth,
                                      position.height);

            var groupBtnRect = new Rect(groupRect.xMax,
                                        position.y,
                                        GroupPopupWidth,
                                        position.height);

            var menuBtnRect  = new Rect(position.xMax - ButtonWidth,
                                        position.y,
                                        ButtonWidth,
                                        position.height);

            // 1) isSource 토글
            EditorGUI.BeginChangeCheck();
            bool isSrc = EditorGUI.Toggle(toggleRect, isSourceProp.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                isSourceProp.boolValue = isSrc;
                property.serializedObject.ApplyModifiedProperties();
            }

            // 2) Value 필드
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(valueRect, valueProp, label, true);
            bool valueChanged = EditorGUI.EndChangeCheck();
            if (valueChanged)
            {
                property.serializedObject.ApplyModifiedProperties();
                SharedUtility.AutoSyncGroup(property);
            }

            // 3) groupId 인라인 텍스트
            string currentGroup = groupProp.stringValue ?? "";
            string displayGroup = string.IsNullOrEmpty(currentGroup)
                ? GetDefaultGroupName(property.propertyPath)   // 비었으면 필드명 기반 기본 그룹
                : currentGroup;

            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginProperty(groupRect, GUIContent.none, groupProp);
            string newGroup = EditorGUI.TextField(groupRect, GUIContent.none,
                string.IsNullOrEmpty(currentGroup) ? "" : currentGroup);
            EditorGUI.EndProperty();

            bool groupChanged = EditorGUI.EndChangeCheck();
            if (groupChanged)
            {
                groupProp.stringValue = newGroup;
                groupProp.serializedObject.ApplyModifiedProperties();
                TrackRecentGroupId(newGroup);
                SharedUtility.AutoSyncGroup(property);
            }

            // groupId 라벨 표시 (옅게)
            if (string.IsNullOrEmpty(currentGroup))
            {
                var labelRect = groupRect;
                labelRect.x += 3f;
                EditorGUI.LabelField(labelRect, displayGroup, EditorStyles.miniLabel);
            }

            // 4) groupId 드롭다운 (기본값/최근값)
            if (GUI.Button(groupBtnRect, "▼", EditorStyles.miniButton))
            {
                ShowGroupIdMenu(property, groupProp);
            }

            // 5) 기존 ... 메뉴 버튼 (Scope / Sync)
            if (GUI.Button(menuBtnRect, "⋯", EditorStyles.miniButton))
            {
                ShowMenu(property, scopeProp, groupProp);
            }

            // 우클릭으로도 ... 메뉴
            var e = Event.current;
            if (e.type == EventType.ContextClick && position.Contains(e.mousePosition))
            {
                ShowMenu(property, scopeProp, groupProp);
                e.Use();
            }
        }

        // ────────────────────────────────────────
        // groupId 드롭다운 (기본 + 최근)
        // ────────────────────────────────────────
        private void ShowGroupIdMenu(SerializedProperty sharedProp, SerializedProperty groupProp)
        {
            EnsureRecentLoaded();

            var menu      = new GenericMenu();
            string path   = sharedProp.propertyPath;
            string defKey = GetDefaultGroupName(path);

            // 기본 = 필드명 기반 그룹
            menu.AddItem(new GUIContent($"Use Field Name ({defKey})"),
                string.IsNullOrEmpty(groupProp.stringValue),
                () =>
                {
                    groupProp.stringValue = "";
                    groupProp.serializedObject.ApplyModifiedProperties();
                    SharedUtility.AutoSyncGroup(sharedProp);
                });

            // 최근 목록
            if (_recentGroupIds.Count > 0)
            {
                menu.AddSeparator("");
                foreach (var g in _recentGroupIds)
                {
                    if (string.IsNullOrEmpty(g))
                        continue;

                    bool selected = groupProp.stringValue == g;
                    menu.AddItem(new GUIContent($"Recent/{g}"), selected, () =>
                    {
                        groupProp.stringValue = g;
                        groupProp.serializedObject.ApplyModifiedProperties();
                        SharedUtility.AutoSyncGroup(sharedProp);
                    });
                }
            }

            menu.ShowAsContext();
        }

        // ────────────────────────────────────────
        // 기존 Scope/Sync 메뉴 + Group 항목 정리
        // ────────────────────────────────────────
        private void ShowMenu(SerializedProperty sharedProp, SerializedProperty scopeProp, SerializedProperty groupProp)
        {
            EnsureRecentLoaded();

            var menu = new GenericMenu();
            var currentScope = (SharedScope)scopeProp.enumValueIndex;

            // Scope
            menu.AddItem(new GUIContent("Scope/GameObject"),
                currentScope == SharedScope.GameObject, () =>
                {
                    scopeProp.enumValueIndex = (int)SharedScope.GameObject;
                    scopeProp.serializedObject.ApplyModifiedProperties();
                });

            menu.AddItem(new GUIContent("Scope/Parent & Children"),
                currentScope == SharedScope.ParentAndChildren, () =>
                {
                    scopeProp.enumValueIndex = (int)SharedScope.ParentAndChildren;
                    scopeProp.serializedObject.ApplyModifiedProperties();
                });

            menu.AddItem(new GUIContent("Scope/Scene"),
                currentScope == SharedScope.Scene, () =>
                {
                    scopeProp.enumValueIndex = (int)SharedScope.Scene;
                    scopeProp.serializedObject.ApplyModifiedProperties();
                });

            menu.AddSeparator("");

            // Group 관련
            string path   = sharedProp.propertyPath;
            string defKey = GetDefaultGroupName(path);

            menu.AddItem(new GUIContent($"Group/Use Field Name ({defKey})"),
                string.IsNullOrEmpty(groupProp.stringValue),
                () =>
                {
                    groupProp.stringValue = "";
                    groupProp.serializedObject.ApplyModifiedProperties();
                    SharedUtility.AutoSyncGroup(sharedProp);
                });

            if (!string.IsNullOrEmpty(groupProp.stringValue))
            {
                menu.AddItem(new GUIContent("Group/Clear Group Id"),
                    false,
                    () =>
                    {
                        groupProp.stringValue = "";
                        groupProp.serializedObject.ApplyModifiedProperties();
                        SharedUtility.AutoSyncGroup(sharedProp);
                    });
            }

            if (_recentGroupIds.Count > 0)
            {
                menu.AddSeparator("Group/");
                foreach (var g in _recentGroupIds)
                {
                    if (string.IsNullOrEmpty(g))
                        continue;

                    bool selected = groupProp.stringValue == g;
                    menu.AddItem(new GUIContent($"Group/Recent/{g}"), selected, () =>
                    {
                        groupProp.stringValue = g;
                        groupProp.serializedObject.ApplyModifiedProperties();
                        SharedUtility.AutoSyncGroup(sharedProp);
                    });
                }
            }

            menu.AddSeparator("");

            // Sync
            menu.AddItem(new GUIContent("Sync/Use This As Source"), false, () =>
            {
                SharedUtility.SyncFromThisAsSource(sharedProp);
            });

            menu.AddItem(new GUIContent("Sync/Auto Source & Sync Group"), false, () =>
            {
                SharedUtility.AutoSyncGroup(sharedProp);
            });

            menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        // ────────────────────────────────────────
        // 헬퍼: 기본 그룹 이름 = 필드명
        // SharedUtility.BuildGroupKey 에서 groupId 비었을 때와 동일한 로직 써야 함
        // ────────────────────────────────────────
        private static string GetDefaultGroupName(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
                return "(unknown)";

            int lastDot = propertyPath.LastIndexOf('.');
            if (lastDot >= 0)
                return propertyPath.Substring(lastDot + 1);

            return propertyPath;
        }

        // ────────────────────────────────────────
        // 헬퍼: 최근 groupId 관리 (EditorPrefs)
        // ────────────────────────────────────────
        private static void EnsureRecentLoaded()
        {
            if (_recentGroupIds != null)
                return;

            _recentGroupIds = new List<string>();
            var raw = EditorPrefs.GetString(RecentKey, "");
            if (string.IsNullOrEmpty(raw))
                return;

            var parts = raw.Split('|');
            foreach (var p in parts)
            {
                var g = p.Trim();
                if (!string.IsNullOrEmpty(g) && !_recentGroupIds.Contains(g))
                    _recentGroupIds.Add(g);
            }
        }

        private static void TrackRecentGroupId(string groupId)
        {
            EnsureRecentLoaded();

            if (string.IsNullOrEmpty(groupId))
                return;

            // 맨 앞에 추가, 중복 제거, 최대 10개 정도
            _recentGroupIds.Remove(groupId);
            _recentGroupIds.Insert(0, groupId);

            const int Max = 10;
            if (_recentGroupIds.Count > Max)
                _recentGroupIds.RemoveRange(Max, _recentGroupIds.Count - Max);

            SaveRecents();
        }

        private static void SaveRecents()
        {
            if (_recentGroupIds == null || _recentGroupIds.Count == 0)
            {
                EditorPrefs.DeleteKey(RecentKey);
                return;
            }

            string raw = string.Join("|", _recentGroupIds);
            EditorPrefs.SetString(RecentKey, raw);
        }
    }
}
#endif
