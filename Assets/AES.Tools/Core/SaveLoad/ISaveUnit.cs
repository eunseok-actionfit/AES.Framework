using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    public interface ISaveUnit
    {
        UniTask LoadAsync(System.Threading.CancellationToken ct = default);
        UniTask SaveAsync(System.Threading.CancellationToken ct = default);
    }
}

