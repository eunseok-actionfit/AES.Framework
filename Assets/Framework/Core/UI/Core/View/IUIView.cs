using UnityEngine;


namespace AES.Tools.Core.View
{
    public interface IUIView
    {
        CanvasGroup CanvasGroup { get; }
        RectTransform Rect { get; }
    }
}