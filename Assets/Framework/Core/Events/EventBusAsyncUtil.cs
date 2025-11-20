using System;
using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    public static class EventBusAsyncUtil
    {
        public static UniTask<T> WaitFor<T>(
            Predicate<T> predicate = null,
            CancellationToken cancellationToken = default
        ) where T : IEvent
        {
            var tcs = new UniTaskCompletionSource<T>();
            
            var binding = new EventBinding<T>()
            {
                OneShot = true,
                Name = $"WaitFor<{typeof(T).Name}>",
                Owner = "EventBusAsyncUtil"
            };
            
            void OnEvent(T e)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    binding.Deregister();
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                if (predicate == null || predicate(e))
                {
                    binding.Deregister();
                    tcs.TrySetResult(e);
                }
            }

            binding.Add(OnEvent);
            binding.Register();

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => {
                    binding.Deregister();
                    tcs.TrySetCanceled(cancellationToken);
                });
            }

            return tcs.Task;
        }
    }
}