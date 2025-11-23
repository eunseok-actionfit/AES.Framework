// #if AESFW_UNITASK
// using Cysharp.Threading.Tasks;
// using Awaitable = Cysharp.Threading.Tasks.UniTask;
// #elif UNITY_2023_1_OR_NEWER
// using Awaitable = UnityEngine.Awaitable;
// #else
// using Awaitable = System.Threading.Tasks.Task;
// #endif
//
// using System;
// using System.Threading;
// using Core.Engine.Factory;
// using Core.Systems.Pooling;
// using UnityEngine;
//
// namespace Core.Systems.Spawning
// {
//     public sealed class ViewSpawner<T> : IViewSpawner<T>, IDisposable where T : Component
//     {
//         readonly ObjectPool<T> _pool;
//         readonly Transform _poolRoot;
//         readonly int _warmup;
//
//         public ViewSpawner(IAssetInstantiator inst,  int capacity = 16, int warmup = 0)
//         {
//             _poolRoot = CreateRoot(typeof(T).Name);
//
//             _pool = new ObjectPool<T>(
//                 factory: new SimpleViewFactory<T>(inst),
//                 capacity: capacity,
//                 onBeforeReturn: v =>
//                 {
//                     var tr = v.transform;
//                     tr.SetParent(_poolRoot, false);
//                     v.gameObject.SetActive(false);
//                 });
//
//             _warmup = warmup;
//         }
//
//         static Transform CreateRoot(string typeName)
//         {
//             var name = $"[PoolRoot]_{typeName}";
//             var go = GameObject.Find(name) ?? new GameObject(name);
//             return go.transform;
//         }
//
//         // 필요하면 게임 쪽에서 수동 호출
//         public async Awaitable StartAsync(CancellationToken ct = default)
//         {
//             if (_warmup > 0)
//                 await _pool.WarmupAsync(_warmup, ct);
//             // AutoShrink 제거: 필요시 직접 Trim 호출
//         }
//
//         public Awaitable<T> Spawn() =>
//             SpawnInternal(Vector3.zero, Quaternion.identity, _poolRoot);
//
//         public Awaitable<T> Spawn(Transform parent) =>
//             SpawnInternal(Vector3.zero, Quaternion.identity, parent ?? _poolRoot);
//
//         public Awaitable<T> Spawn(Vector3 p, Quaternion r) =>
//             SpawnInternal(p, r, _poolRoot);
//
//         public Awaitable<T> Spawn(Vector3 p, Quaternion r, Transform parent) =>
//             SpawnInternal(p, r, parent ?? _poolRoot);
//
//         async Awaitable<T> SpawnInternal(Vector3 pos, Quaternion rot, Transform parent)
//         {
// #if AESFW_UNITASK
//             await Cysharp.Threading.Tasks.UniTask.SwitchToMainThread();
// #elif UNITY_2023_1_OR_NEWER
//             await Awaitable.MainThreadAsync();
// #else
//             // Task 환경이면, 메인 스레드 디스패처를 사용하거나
//             // 이미 메인 스레드에서만 호출된다고 가정
// #endif
//             var v = await _pool.Rent();
//             var tr = v.transform;
//             tr.SetParent(parent, false);
//             tr.localPosition = pos;
//             tr.localRotation = rot;
//             v.gameObject.SetActive(true);
//             return v;
//         }
//
//         public void Despawn(T v) => _pool.Return(v);
//
//         public void Dispose()
//         {
//             _pool.Dispose();
//         }
//     }
// }
