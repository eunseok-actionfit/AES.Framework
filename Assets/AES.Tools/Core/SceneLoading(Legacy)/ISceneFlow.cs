using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    public interface ISceneFlow
    {
        UniTask LoadWithArgsAsync<T>(
            string sceneName,
            string channel,
            T payload,
            bool additive = false,
            string overlayAddressKey = null,
            bool allowActivation = true,
            CancellationToken ct = default)  where T : class;
        
        UniTask GoHome(string overlayKey = null, CancellationToken ct = default);
        UniTask GoGame(ISceneArgs args, string overlayKey = null, CancellationToken ct = default);
    }
}