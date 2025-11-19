using UnityEngine;


namespace Core.Systems.UI.Core.UILayer
{
    public interface IExtraInsetsProvider
    {
        /// <summary> 픽셀 단위 추가 인셋(RectOffset: left, right, top, bottom) </summary>
        RectOffset GetExtraInsets();
    }
}