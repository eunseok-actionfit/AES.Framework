using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    /// <summary>
    /// 팩토리 인터페이스
    /// - 풀에서 객체를 생성/파기할 때 사용
    /// </summary>
    public interface IAsyncFactory<T>
    {
        UniTask<T> CreateAsync(CancellationToken ct = default);
        void Destroy(T item); // 반드시 메인스레드에서 호출되도록 구현
    }
}