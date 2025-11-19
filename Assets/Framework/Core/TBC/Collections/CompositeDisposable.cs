using System;
using System.Collections.Generic;


namespace Core.Engine.EventBus
{
    /// <summary>
    /// 여러 IDisposable을 묶어서 한 번에 해제하는 컨테이너.
    /// Rx의 CompositeDisposable과 유사한 개념.
    /// </summary>
    public sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _list = new();
        private bool _disposed;

        /// <summary>
        /// IDisposable 추가.
        /// 이미 Dispose된 상태라면 즉시 Dispose.
        /// </summary>
        public T Add<T>(T disposable) where T : IDisposable
        {
            if (disposable == null)
                return default;

            if (_disposed)
            {
                disposable.Dispose();
                return disposable;
            }

            _list.Add(disposable);
            return disposable;
        }

        /// <summary>
        /// 개별 항목 제거 및 Dispose.
        /// </summary>
        public bool Remove(IDisposable disposable)
        {
            if (disposable == null)
                return false;

            if (_list.Remove(disposable))
            {
                disposable.Dispose();
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var d in _list)
            {
                try
                {
                    d.Dispose();
                }
                catch (Exception)
                {
                    // 필요하면 로그 처리 추가 가능
                }
            }

            _list.Clear();
        }
    }

    /// <summary>
    /// Rx 스타일 AddTo 확장 메서드.
    /// </summary>
    public static class DisposableExtensions
    {
        public static T AddTo<T>(this T disposable, CompositeDisposable composite)
            where T : IDisposable
        {
            composite?.Add(disposable);
            return disposable;
        }
    }
}