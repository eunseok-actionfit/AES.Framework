using AES.Tools.TBC.Unit;
using Cysharp.Threading.Tasks;


namespace AES.Tools.UI.Popups
{

    public interface IPopupService
    {
        // 결과 있는 팝업
        UniTask<TResult> Show<TView, TResult>(PopupViewModelBase<TResult> vm, bool modal = true)
            where TView : PopupViewBase;

        bool TryShow<TView, TResult>(PopupViewModelBase<TResult> vm, out UniTask<TResult> task, bool modal = true)
            where TView : PopupViewBase;

        // 결과 없는 팝업 (Unit을 호출부에서 안 보이게)
        UniTask Show<TView>(PopupViewModelBase<Unit> vm, bool modal = true)
            where TView : PopupViewBase;

        bool TryShow<TView>(PopupViewModelBase<Unit> vm, out UniTask task, bool modal = true)
            where TView : PopupViewBase;

        void CloseTop();
        bool IsOpen<TView>() where TView : PopupViewBase;
    }


    public sealed class PopupService : IPopupService
    {
        readonly PopupHost _host;
        public PopupService(PopupHost host) => _host = host;

        public UniTask<TResult> Show<TView, TResult>(PopupViewModelBase<TResult> vm, bool modal = true)
            where TView : PopupViewBase
            => _host.ShowAsync<TView, TResult>(vm, modal);

        public bool TryShow<TView, TResult>(PopupViewModelBase<TResult> vm, out UniTask<TResult> task, bool modal = true)
            where TView : PopupViewBase
            => _host.TryShowAsync<TView, TResult>(vm, out task, modal);

        public async UniTask Show<TView>(PopupViewModelBase<Unit> vm, bool modal = true)
            where TView : PopupViewBase
        {
            await _host.ShowAsync<TView, Unit>(vm, modal);
        }

        public bool TryShow<TView>(PopupViewModelBase<Unit> vm, out UniTask task, bool modal = true)
            where TView : PopupViewBase
        {
            if (_host.TryShowAsync<TView, Unit>(vm, out var typed, modal))
            {
                task = typed.AsUniTask(); // UniTask<Unit> -> UniTask
                return true;
            }
            task = default;
            return false;
        }

        public void CloseTop() => _host.CloseTop();
        public bool IsOpen<TView>() where TView : PopupViewBase => _host.IsOpen<TView>();
    }
}