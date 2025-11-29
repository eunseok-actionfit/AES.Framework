using System;

namespace AES.Tools
{
    /// <summary>
    /// 런타임/에디터에서 바인딩 컨텍스트를 제공하는 공통 인터페이스.
    /// DataContextBase, PopupView 등에서 구현해서 사용.
    /// </summary>
    public interface IBindingContextProvider
    {
        /// <summary>런타임에 바인딩이 사용할 컨텍스트</summary>
        IBindingContext RuntimeContext { get; }

#if UNITY_EDITOR
        /// <summary>에디터 드롭다운을 위해 사용할 ViewModel 타입</summary>
        Type DesignTimeViewModelType { get; }

        /// <summary>필요하면 디자인타임 ViewModel 인스턴스 (없으면 null 가능)</summary>
        object GetDesignTimeViewModel();
#endif
    }
}