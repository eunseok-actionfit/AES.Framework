using System;
using Cysharp.Threading.Tasks;


namespace AES.Tools.VContainer.Services.Loading
{
    public interface ILoadingService
    {
        UniTask RunWithLoadingAsync(Func<UniTask> loadFlow, string overlayAddressKey, float minShowSeconds = 0.35f);

        UniTask RunWithLoadingAsync(Func<IProgress<float>, UniTask> loadFlow,
                                    string overlayAddressKey,
                                    float minShowSeconds = 0.35f,
                                    IProgress<string> message = null);
    }
}