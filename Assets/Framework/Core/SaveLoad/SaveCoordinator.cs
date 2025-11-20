using System;
using System.Threading;
using AES.Tools;
using AES.Tools.Core;
using Cysharp.Threading.Tasks;


public sealed class SaveCoordinator : ISaveCoordinator
{
    private readonly IStorageService storage;
    private readonly ISlotService slot;

    public SaveCoordinator(IStorageService storage, ISlotService slot)
    {
        this.storage = storage;
        this.slot = slot;
    }

    public async UniTask<Result> LoadAllAsync(CancellationToken ct = default)
    {
        EventBus<SaveStartedEvent>.Raise(new SaveStartedEvent { IsSave = false });

        foreach (var info in SaveDataRegistry.All)
        {
            var method = typeof(IStorageService)
                .GetMethod("LoadAsync")
                ?.MakeGenericMethod(info.Type);

            if (method != null)
            {
                var taskObj = method.Invoke(storage, new object[] { slot.CurrentSlotId, ct });

                // UniTask<Result<T>>.AsUniTask() 호출해서 non-generic UniTask로 변환
                var asUniTaskMethod = taskObj.GetType().GetMethod("AsUniTask");

                if (asUniTaskMethod != null)
                {
                    var uniTask = (UniTask)asUniTaskMethod.Invoke(taskObj, null);

                    // 결과는 지금은 버려도 됨 (필요하면 나중에 generic Result<T> 빼올 수 있음)
                    await uniTask;
                }

            }
        }

        var result = Result.Ok();
        EventBus<SaveCompletedEvent>.Raise(new SaveCompletedEvent { IsSave = false, Result = result });
        return result;
    }

    public async UniTask<Result> SaveAllAsync(CancellationToken ct = default)
    {
        EventBus<SaveStartedEvent>.Raise(new SaveStartedEvent { IsSave = true });

        foreach (var info in SaveDataRegistry.All)
        {
            // TODO: 실제 게임 서비스에서 Data 인스턴스를 가져오는 Hook 필요
            var data = Activator.CreateInstance(info.Type);

            var method = typeof(IStorageService)
                .GetMethod("SaveAsync")
                ?.MakeGenericMethod(info.Type);

            if (method != null)
            {
                var task = (UniTask<Result>)method.Invoke(storage, new object[] { slot.CurrentSlotId, data, ct });
                var r = await task;
                if (r.IsFail)
                {
                    EventBus<SaveFailedEvent>.Raise(new SaveFailedEvent { IsSave = true, Error = r.Error });
                    return r;
                }
            }

        }

        var ok = Result.Ok();
        EventBus<SaveCompletedEvent>.Raise(new SaveCompletedEvent { IsSave = true, Result = ok });
        return ok;
    }
}