using System;

namespace AES.Tools
{
    /// <summary>
    /// ViewModel(혹은 POCO 데이터)을 path 기반으로 접근하기 위한 공통 컨텍스트 인터페이스.
    /// 순수 C#이며, Unity 의존성 없음.
    /// </summary>
    public interface IBindingContext
    {
        /// <summary>path에 해당하는 현재 값을 반환. path가 null/빈 문자열이면 root 자체를 반환.</summary>
        object GetValue(string path = null);

        /// <summary>path에 값을 설정. (지원 가능한 경우에만 유효)</summary>
        void SetValue(string path, object value);

        /// <summary>
        /// 값 변경을 구독. 필요하면 토큰을 반환(없으면 null).
        /// 동일 path에 여러 콜백 구독 가능.
        /// </summary>
        object RegisterListener(string path, Action<object> onValueChanged);
        

        /// <summary>
        /// RegisterListener에서 사용한 path/콜백/토큰 조합으로 구독 해제.
        /// </summary>
        void RemoveListener(string path, Action<object> onValueChanged, object token = null);
    }
}