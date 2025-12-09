#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using UnityEngine;

public class AdsProfileOdinDrawer : OdinValueDrawer<AdsProfile>
{
    protected override void DrawPropertyLayout(GUIContent label)
    {
        var environment = this.ValueEntry.SmartValue.environment.ToString();
        var platform    = this.ValueEntry.SmartValue.platform.ToString();

        var newLabel = new GUIContent($"{environment} / {platform}");

        CallNextDrawer(newLabel);
    }
}
#endif