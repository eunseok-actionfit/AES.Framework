using UnityEngine;


namespace AES.Tools.Layer
{
    public interface IExtraInsetsProvider
    {
        /// <summary> 픽셀 단위 추가 인셋(RectOffset: left, right, top, bottom) </summary>
        RectOffset GetExtraInsets();
    }
}