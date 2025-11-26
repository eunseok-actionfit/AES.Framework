// using System;
// using System.Linq;
// using System.Threading;
// using AES.Tools;
// using AES.Tools.Core;
// using Cysharp.Threading.Tasks;
//
//
// public sealed class SaveCoordinator : ISaveCoordinator
// {
//     private readonly IStorageService storage;
//     private readonly ISlotService slot;
//
//     public SaveCoordinator(IStorageService storage, ISlotService slot)
//     {
//         this.storage = storage;
//         this.slot = slot;
//     }
//
//     public async UniTask<Result> LoadAllAsync(CancellationToken ct = default)
//     {
//         EventBus<SaveStartedEvent>.Raise(new SaveStartedEvent { IsSave = false });
//
//         var loadGenericDef = typeof(IStorageService)
//             .GetMethods()
//             .Single(m =>
//                 m.Name == "LoadAsync" &&
//                 m.IsGenericMethodDefinition &&
//                 m.GetParameters().Length == 2 &&           // slotId, ct
//                 m.GetParameters()[0].ParameterType == typeof(string) &&
//                 m.GetParameters()[1].ParameterType == typeof(CancellationToken)
//             );
//
//         foreach (var info in SaveDataRegistry.All)
//         {
//             var method = loadGenericDef.MakeGenericMethod(info.Type);
//
//             var taskObj = method.Invoke(storage, new object[] { slot.CurrentSlotId, ct });
//             if (taskObj == null)
//                 continue;
//
//             var asUniTaskMethod = taskObj.GetType().GetMethod("AsUniTask");
//             if (asUniTaskMethod == null)
//                 continue;
//
//             var uniTask = (UniTask)asUniTaskMethod.Invoke(taskObj, null);
//             await uniTask;
//         }
//
//         var result = Result.Ok();
//         EventBus<SaveCompletedEvent>.Raise(new SaveCompletedEvent { IsSave = false, Result = result });
//         return result;
//     }
//
//
//     public async UniTask<Result> SaveAllAsync(CancellationToken ct = default)
//     {
//         EventBus<SaveStartedEvent>.Raise(new SaveStartedEvent { IsSave = true });
//
//         // SaveAsync<T>(string slotId, T data, CancellationToken ct) 이 시그니처만 선택
//         var saveGenericDef = typeof(IStorageService)
//             .GetMethods()
//             .Single(m =>
//                 m.Name == "SaveAsync" &&
//                 m.IsGenericMethodDefinition &&
//                 m.GetParameters().Length == 3 &&           // slotId, data, ct
//                 m.GetParameters()[0].ParameterType == typeof(string) &&
//                 m.GetParameters()[2].ParameterType == typeof(CancellationToken)
//             );
//
//         foreach (var info in SaveDataRegistry.All)
//         {
//             var data = /* 여기서 실제 게임 데이터 가져오기 (2번에서 설명) */;
//
//             var saveMethod = saveGenericDef.MakeGenericMethod(info.Type);
//
//             var task = (UniTask<Result>)saveMethod.Invoke(storage, new object[]
//             {
//                 slot.CurrentSlotId,
//                 data,
//                 ct
//             });
//
//             var r = await task;
//             if (r.IsFail)
//             {
//                 EventBus<SaveFailedEvent>.Raise(new SaveFailedEvent { IsSave = true, Error = r.Error });
//                 return r;
//             }
//         }
//
//         var ok = Result.Ok();
//         EventBus<SaveCompletedEvent>.Raise(new SaveCompletedEvent { IsSave = true, Result = ok });
//         return ok;
//     }
// }