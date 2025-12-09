#if UNITY_EDITOR && ODIN_INSPECTOR
using AES.Tools;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using UnityEngine;

namespace AES.Tools.Gui.Editor
{
    public class AesLabelTextOdinDrawer : OdinAttributeDrawer<AesLabelTextAttribute>
    {
        protected override void DrawPropertyLayout(GUIContent label)
        {
            // 배열/리스트의 요소(Element 0, 1, ...)인지 체크
            bool isCollectionElement =
                this.Property.Parent != null &&
                this.Property.Parent.ChildResolver is ICollectionResolver;   // 컬렉션 내부 요소에만 값이 들어옴

            if (isCollectionElement)
            {
                // 요소는 원래 라벨 그대로 사용
                this.CallNextDrawer(label);
                return;
            }

            var attr = this.Attribute;
            var custom = string.IsNullOrEmpty(attr.Text)
                ? label
                : new GUIContent(attr.Text, label?.tooltip);

            this.CallNextDrawer(custom);
        }
    }
}
#endif