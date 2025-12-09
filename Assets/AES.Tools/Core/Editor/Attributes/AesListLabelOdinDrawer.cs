#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace AES.Tools.Gui.Editor
{
    public class AesListLabelOdinDrawer : OdinAttributeDrawer<AesListLabelAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (!Property.ChildResolver.IsCollection)
            {
                CallNextDrawer(label);
                return;
            }

            if (label == null)
                label = GUIContent.none;

            SirenixEditorGUI.BeginBox();
            SirenixEditorGUI.BeginBoxHeader();
            GUILayout.Label(label);
            SirenixEditorGUI.EndBoxHeader();

            EditorGUI.indentLevel++;

            string expr = Attribute.Expression;

            for (int i = 0; i < Property.Children.Count; i++)
            {
                var item      = Property.Children[i];
                string labStr = Evaluate(item, expr);

                item.Draw(new GUIContent(labStr));
            }

            EditorGUI.indentLevel--;

            SirenixEditorGUI.EndBox();
        }

        private string Evaluate(InspectorProperty item, string expr)
        {
            if (string.IsNullOrEmpty(expr))
                return item.NiceName;

            if (expr.StartsWith("@"))
                expr = expr.Substring(1);

            string output = "";
            var parts = expr.Split('+');

            foreach (var raw in parts)
            {
                var s = raw.Trim();
                if (s.Length == 0)
                    continue;

                if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
                {
                    output += s.Substring(1, s.Length - 2);
                    continue;
                }

                // 여기만 람다로 변경
                var child = item.FindChild(p => p.Name == s, true);

                if (child != null && child.ValueEntry != null)
                {
                    var v = child.ValueEntry.WeakSmartValue;
                    if (v != null)
                        output += v.ToString();
                }
            }

            return string.IsNullOrEmpty(output) ? item.NiceName : output;
        }
    }
}
#endif
