using UnityEngine;


namespace AES.Tools.Core
{
    public interface IUIView
    {
        CanvasGroup CanvasGroup { get; }
        RectTransform Rect { get; }
    }
}