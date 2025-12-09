#if UNITY_EDITOR && ODIN_INSPECTOR
using AES.Tools;
using AES.Tools.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace AES.Tools.Gui.Editor
{
    public class AesGUIColorOdinDrawer : OdinAttributeDrawer<AesGUIColorAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var target = Property.ParentValues[0];
            var prev = GUI.color;

            if (AesGUIColorHelper.TryGetColor(target, Attribute, out var col))
                GUI.color = col;

            CallNextDrawer(label);

            GUI.color = prev;
        }
    }
}
#endif