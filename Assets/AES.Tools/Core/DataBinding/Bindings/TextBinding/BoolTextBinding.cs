using UnityEngine;


namespace AES.Tools
{
    /// <summary>
    /// ObservableProperty&lt;bool&gt; 전용 TextBinding
    /// true/false 를 원하는 문자열로 매핑한 뒤,
    /// 그 결과 문자열에 대해 다시 포맷을 적용한다.
    /// 예:
    ///   trueText = "예", falseText = "아니요"
    ///   useFormat = true, format = "[{0}]"
    ///   true  → "[예]"
    ///   false → "[아니요]"
    /// </summary>
    public class BoolTextBinding : GenericTextBinding<bool>
    {
    }
}


