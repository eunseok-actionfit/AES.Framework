using UnityEngine;


namespace AES.Tools.UI_Regacy_.Core.View
{
    public interface IUIView
    {
        CanvasGroup CanvasGroup { get; }
        RectTransform Rect { get; }
    }
}