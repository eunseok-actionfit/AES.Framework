using System;
using System.Threading;
using Core.Systems.UI;
using Core.Systems.UI.Core.UILayer;
using Core.Systems.UI.Core.UIManager;
using Core.Systems.UI.Core.UIRoot;
using Core.Systems.UI.Core.UIView;
using Cysharp.Threading.Tasks;


public static class UI
{
    public static IUIController Controller => UiServices.UIController;
  
    public static UniTask<UIView> ShowAsync<TEnum>(TEnum id, object model = null, CancellationToken ct = default) where TEnum : Enum 
        => Controller.ShowAsync(id, model, ct);

    public static UniTask HideAsync<TEnum>(TEnum id, CancellationToken ct = default) where TEnum : Enum 
        => Controller.HideAsync(id, ct);

    public static UniTask CloseAllAsync(UIRootRole scope, CancellationToken ct = default)
        => Controller.CloseAllAsync(scope, ct);
}