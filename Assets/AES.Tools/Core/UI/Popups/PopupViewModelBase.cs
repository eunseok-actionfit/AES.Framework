using System;
using AES.Tools.TBC.Unit;
using Cysharp.Threading.Tasks;


namespace AES.Tools.UI.Popups
{
    // 팝업 VM은 Close(result)로 종료 신호를 내고, Host는 Result를 await 한다.
    public abstract class PopupViewModelBase<TResult> : IDisposable
    {
        private UniTaskCompletionSource<TResult> _tcs;
        private bool _used;

        protected PopupViewModelBase()
        {
            Reset();
        }

        protected void Reset()
        {
            _tcs = new UniTaskCompletionSource<TResult>();
            _used = false;
        }

        public UniTask<TResult> Result
        {
            get
            {
                if (_used)
                    throw new InvalidOperationException("PopupViewModel은 재사용할 수 없습니다.");
                return _tcs.Task;
            }
        }

        public void Close(TResult result = default)
        {
            _used = true;
            _tcs.TrySetResult(result);
        }

        public virtual void Dispose() { }
    }

    
    public abstract class PopupViewModelBase : PopupViewModelBase<Unit>
    {
        public void Close() => Close(Unit.Default);
    }
}