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
                ToastModel model = new ToastModel { Message = message };
                // 새로운 토스트 인스턴스 생성 (Multi 정책)
                var toastView = await UI.ShowInstanceAsync(GlobalUIId.Toast, model);

                // duration 동안 대기
                await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct);

                // 해당 인스턴스만 닫기
                await UI.HideInstanceAsync(toastView);
            }
            catch (OperationCanceledException)
            {
                // 취소됨
            }
        }
    }
}