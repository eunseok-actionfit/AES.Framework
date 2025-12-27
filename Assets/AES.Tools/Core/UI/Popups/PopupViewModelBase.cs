using AES.Tools.TBC.Unit;
using Cysharp.Threading.Tasks;


namespace AES.Tools.UI.Popups
{
    // 팝업 VM은 Close(result)로 종료 신호를 내고, Host는 Result를 await 한다.
    public abstract class PopupViewModelBase<TResult>
    {
        private readonly UniTaskCompletionSource<TResult> _tcs = new();

        public UniTask<TResult> Result => _tcs.Task;

        public void Close(TResult result = default) => _tcs.TrySetResult(result);
    }
    
    public abstract class PopupViewModelBase : PopupViewModelBase<Unit>
    {
        public void Close() => Close(Unit.Default);
    }
}