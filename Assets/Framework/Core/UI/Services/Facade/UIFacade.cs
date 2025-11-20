using System;
using System.Threading;
using AES.Tools.Core.Controller;
using AES.Tools.Core.Root;
using AES.Tools.Core.View;
using AES.Tools.Services.Infrastructure;
using Cysharp.Threading.Tasks;


namespace AES.Tools.Services.Facade
{
    public static class UI
    {
        private static IUIController Controller => UiServiceLocator.UIController;

        public static UniTask<UIView> ShowAsync<TEnum>(TEnum id, object model = null, CancellationToken ct = default)
            where TEnum : Enum => Controller.ShowAsync(id, model, ct);

        public static UniTask HideAsync<TEnum>(TEnum id, CancellationToken ct = default)
            where TEnum : Enum => Controller.HideAsync(id, ct);

        public static UniTask CloseAllAsync(UIRootRole scope, CancellationToken ct = default)
            => Controller.CloseAllAsync(scope, ct);

        public static UniTask<UIView> ShowInstanceAsync<TEnum>(TEnum id, object model = null, CancellationToken ct = default)
            where TEnum : Enum => Controller.ShowInstanceAsync(id, model, ct);

        public static UniTask HideInstanceAsync(UIView view, CancellationToken ct = default)
            => Controller.HideInstanceAsync(view, ct);
    }
}