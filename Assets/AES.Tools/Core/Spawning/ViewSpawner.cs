// using System;
// using System.Threading;
// using AES.Tools;
// using Cysharp.Threading.Tasks;
// using UnityEngine;
//
//
// namespace Core.Systems.Spawning
// {
//     public sealed class ViewSpawner<T> : IViewSpawner<T>, IDisposable where T : Component
//     {
//         readonly ObjectPool<T> _pool;
//         readonly Transform _poolRoot;
//         readonly int _warmup;
//
//         public ViewSpawner(IAssetProvider assetProvider, int capacity = 16, int warmup = 0)
//         {
//             _poolRoot = CreateRoot(typeof(T).Name);
//
//             _pool = new ObjectPool<T>(
//                 factory: new SimpleViewFactory<T>(assetProvider),
//                 capacity: capacity,
//                 onBeforeReturn: v => {
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
//         public async UniTask WarmupAsync(CancellationToken ct = default)
//         {
//             if (_warmup > 0)
//                 await _pool.WarmupAsync(_warmup, ct);
//             // AutoShrink 제거: 필요시 직접 Trim 호출
//         }
//
//         public UniTask<T> Spawn() =>
//             SpawnInternal(Vector3.zero, Quaternion.identity, _poolRoot);
//
//         public UniTask<T> Spawn(Transform parent) =>
//             SpawnInternal(Vector3.zero, Quaternion.identity, parent ?? _poolRoot);
//
//         public UniTask<T> Spawn(Vector3 p, Quaternion r) =>
//             SpawnInternal(p, r, _poolRoot);
//
//         public UniTask<T> Spawn(Vector3 p, Quaternion r, Transform parent) =>
//             SpawnInternal(p, r, parent ?? _poolRoot);
//
//         async UniTask<T> SpawnInternal(Vector3 pos, Quaternion rot, Transform parent)
//         {
//             await UniTask.SwitchToMainThread();
//
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