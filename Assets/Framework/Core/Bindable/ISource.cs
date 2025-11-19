using System;


namespace UnityUtils.Bindable
{
    public interface IReadableSource<T>
    {
        T Get();
        /// <summary>(old, @new)</summary>
        event Action<T, T> Changed;
        /// <summary>외부가 재발행을 원할 때(옵션)</summary>
        void Refresh();
    }

    public interface IWritableSource<T> : IReadableSource<T>
    {
        /// <summary>실제 값이 바뀌면 true</summary>
        bool TrySet(T value);
    }
}