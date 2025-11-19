using System.Threading;

#if AESFW_UNITASK
using Cysharp.Threading.Tasks;
using Awaitable = Cysharp.Threading.Tasks.UniTask;      // 비제네릭용 별칭
#elif UNITY_2023_1_OR_NEWER
using UnityEngine;                                     // UnityEngine.Awaitable / Awaitable<T>
#else
using System.Threading.Tasks;
using Awaitable = System.Threading.Tasks.Task;        // 비제네릭용 별칭
#endif

namespace Core.Engine.Factory
{
    /// <summary>
    /// 팩토리 인터페이스
    /// - 풀에서 객체를 생성/파기할 때 사용
    /// </summary>
    public interface IAsyncFactory<T>
    {
#if AESFW_UNITASK
        UniTask<T> CreateAsync(CancellationToken ct = default);
#elif UNITY_2023_1_OR_NEWER
        Awaitable<T> CreateAsync(CancellationToken ct = default);
#else
        Task<T> CreateAsync(CancellationToken ct = default);
#endif
        void Destroy(T item); // 반드시 메인스레드에서 호출되도록 구현
    }
}