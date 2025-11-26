using UnityEngine;


namespace AES.Tools.View
{
    public interface IUIView
    {
        CanvasGroup CanvasGroup { get; }
        RectTransform Rect { get; }
    }
}