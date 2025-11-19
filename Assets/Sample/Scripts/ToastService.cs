using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Sample.Scripts
{
    public sealed class ToastService
    {
        
        public async UniTask ShowAsync(string message, float seconds = 1.2f, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            
            
            try
            {
                // 1) 새로운 토스트 표시
                await UI.ShowAsync(GlobalUIId.Toast, message);

                // 2) duration 동안 대기
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct);

                // 3) 토스트 닫기
                await UI.HideAsync(GlobalUIId.Toast);
            }
            catch (OperationCanceledException)
            {
              
            }
        }
    }
}