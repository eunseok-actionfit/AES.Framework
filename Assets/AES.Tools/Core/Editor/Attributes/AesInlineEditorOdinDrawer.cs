#if UNITY_EDITOR && ODIN_INSPECTOR
using AES.Tools;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Gui.Editor
{
    public class AesInlineEditorOdinDrawer : OdinAttributeDrawer<AesInlineEditorAttribute>
    {
        private UnityEditor.Editor _inlineEditor;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            CallNextDrawer(label);

            Object obj = Property.ValueEntry.WeakSmartValue as Object;
            if (obj == null)
                return;

            if (_inlineEditor == null || _inlineEditor.target != obj)
                UnityEditor.Editor.CreateCachedEditor(obj, null, ref _inlineEditor);

            if (_inlineEditor == null)
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (Attribute.DrawHeader)
                EditorGUILayout.LabelField(obj.name, EditorStyles.boldLabel);

            if (Attribute.DrawPreview)
            {
                var preview = _inlineEditor.HasPreviewGUI();
                if (preview)
                {
                    float h = Mathf.Max(64f, EditorGUIUtility.singleLineHeight * 4f);
                    Rect r = GUILayoutUtility.GetRect(0, h, GUILayout.ExpandWidth(true));
                    _inlineEditor.OnPreviewGUI(r, EditorStyles.helpBox);
                }
            }

            _inlineEditor.OnInspectorGUI();
            EditorGUILayout.EndVertical();
        }
    }
}
#endif