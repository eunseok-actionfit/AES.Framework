using UnityEngine;


namespace Core.Systems.UI.Core.UIView
{
    public interface IUIView
    {
        CanvasGroup CanvasGroup { get; }
        RectTransform Rect { get; }
    }
}