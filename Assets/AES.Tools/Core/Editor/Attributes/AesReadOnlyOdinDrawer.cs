#if UNITY_EDITOR && ODIN_INSPECTOR
using AES.Tools;
using AES.Tools.Gui;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace AES.Tools.Gui.Editor
{
    public class AesReadOnlyOdinDrawer : OdinAttributeDrawer<AesReadOnlyAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var prev = GUI.enabled;
            GUI.enabled = false;

            // 기본 Odin 드로잉 호출
            CallNextDrawer(label);

            GUI.enabled = prev;
        }
    }
}
#endif