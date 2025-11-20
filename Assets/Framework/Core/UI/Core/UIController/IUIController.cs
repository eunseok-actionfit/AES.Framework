using System;
using System.Threading;
using AES.Tools.Core.UIRoot;
using Cysharp.Threading.Tasks;


namespace AES.Tools.Core.UIController
{
    public enum UILayerKind
    {
        Window, Hud, Popup, Overlay
    }
    
    public interface IUIController
    {
        // 풀 관련
        void EnsurePool<TEnum>(TEnum id, int capacity = 8, int warmUp = 0)
            where TEnum : Enum;

        void EnsureAllPools(UIRootRole fallbackRole = UIRoot.UIRootRole.Global);

        // 런타임 정책 수정

        // 기본 Show/Hide
        UniTask<UIView.UIView> ShowAsync<TEnum>(TEnum id, object model = null, CancellationToken ct = default)
            where TEnum : Enum;

        UniTask HideAsync<TEnum>(TEnum id, CancellationToken ct = default)
            where TEnum : Enum;

        UniTask CloseAllAsync(UIRootRole scope, CancellationToken ct = default);
        
        bool IsOpen<TEnum>(TEnum id)
            where TEnum : Enum;

        // 인스턴스 단위 Show/Hide
        UniTask<UIView.UIView> ShowInstanceAsync<TEnum>(TEnum id, object model = null, CancellationToken ct = default)
            where TEnum : Enum;

        UniTask HideInstanceAsync(UIView.UIView view, CancellationToken ct = default);

        // Back 키 처리
        void OnBackKey();
    }
}