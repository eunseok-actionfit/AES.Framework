using UnityEngine;


namespace AES.Tools.Core.UIView
{
    public interface IUIView
    {
        CanvasGroup CanvasGroup { get; }
        RectTransform Rect { get; }
    }
}