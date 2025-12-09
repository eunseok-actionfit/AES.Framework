#if UNITY_EDITOR && ODIN_INSPECTOR
using AES.Tools;
using AES.Tools.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace AES.Tools.Gui.Editor
{
    public class AesShowIfOdinDrawer : OdinAttributeDrawer<AesShowIfAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var target = Property.ParentValues[0];
            bool cond = AesConditionEvaluator.EvaluateRaw(target, Attribute);

            if (!cond)
                return;

            CallNextDrawer(label);
        }
    }

    public class AesHideIfOdinDrawer : OdinAttributeDrawer<AesHideIfAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var target = Property.ParentValues[0];
            bool cond = AesConditionEvaluator.EvaluateRaw(target, Attribute);

            if (cond)
                return;

            CallNextDrawer(label);
        }
    }

    public class AesEnableIfOdinDrawer : OdinAttributeDrawer<AesEnableIfAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var target = Property.ParentValues[0];
            bool cond = AesConditionEvaluator.EvaluateRaw(target, Attribute);

            bool prev = GUI.enabled;
            GUI.enabled = prev && cond;

            CallNextDrawer(label);

            GUI.enabled = prev;
        }
    }

    public class AesDisableIfOdinDrawer : OdinAttributeDrawer<AesDisableIfAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            var target = Property.ParentValues[0];
            bool cond = AesConditionEvaluator.EvaluateRaw(target, Attribute);

            bool prev = GUI.enabled;
            GUI.enabled = prev && !cond;

            CallNextDrawer(label);

            GUI.enabled = prev;
        }
    }
}
#endif