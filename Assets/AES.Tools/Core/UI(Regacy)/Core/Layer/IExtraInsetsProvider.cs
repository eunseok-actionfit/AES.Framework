using UnityEngine;


namespace AES.Tools.UI_Regacy_.Core.Layer
{
    public interface IExtraInsetsProvider
    {
        /// <summary> 픽셀 단위 추가 인셋(RectOffset: left, right, top, bottom) </summary>
        RectOffset GetExtraInsets();
    }
}