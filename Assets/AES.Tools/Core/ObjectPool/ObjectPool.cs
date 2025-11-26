using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

#if AESFW_UNITASK


#elif UNITY_2023_1_OR_NEWER
using Awaitable = UnityEngine.Awaitable;
using AwaitableVoid = UnityEngine.Awaitable;


#else
using Awaitable = System.Threading.Tasks.Task;
using AwaitableVoid = System.Threading.Tasks.Task;
#endif

namespace AES.Tools
{
    /// <summary>
    /// 유니티 오브젝트용 범용 풀 구현.
    /// - 생성/파괴는 메인 스레드에서만 수행
    /// - 여유 객체는 Capacity를 넘지 않도록 정리
    /// </summary>
    /// <typeparam name="T">풀링할 유니티 오브젝트 타입</typeparam>
    public class ObjectPool<T> : IGameObjectPool where T : Object
    {
        // 동기화 락
        private readonly object _sync = new();

        // 비동기 생성 팩토리
        private readonly IAsyncFactory<T> _factory;

        // 대기열(Free)
        private readonly Queue<T> _free = new();

        // 사용 중(InUse)
        private readonly HashSet<T> _inUse = new();

        readonly Action<T> _onBeforeReturn;
        readonly Action<T> _onAfterRent;

        /// <summary>풀 목표 용량</summary>
        public int Capacity { get; }

        /// <summary>여유 객체 수</summary>
        public int CountFree
        {
            get
            {
                lock (_sync) return _free.Count;
            }
        }

        /// <summary>대여 중 객체 수</summary>
        public int CountInUse
        {
            get
            {
                lock (_sync) return _inUse.Count;
            }
        }

        // 해제 여부
        private bool _disposed;

        // -------- Diagnostics (필요 최소한만) --------

        /// <summary>누적 생성 수</summary>
        public int CreatedCount { get; private set; }

        /// <summary>누적 파괴 수</summary>
        public int DestroyedCount { get; private set; }


        /// <summary>
        /// 풀을 초기화한다.
        /// </summary>
        public ObjectPool(
            IAsyncFactory<T> factory,
            int capacity = 8,
            Action<T> onBeforeReturn = null,
            Action<T> onAfterRent = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            Capacity = capacity;
            _onBeforeReturn = onBeforeReturn;
            _onAfterRent = onAfterRent;
        }

        // -------- Warmup / Trim --------

        /// <summary>
        /// 지정 수량만큼 미리 생성한다.
        /// </summary>
        public async UniTask WarmupAsync(int warmUp, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var target = Math.Clamp(warmUp, 0, Capacity);
            var need = target - (CountFree + CountInUse);
            if (need <= 0) return;

            for (int i = 0; i < need; i++)
            {
                ct.ThrowIfCancellationRequested();

#if AESFW_UNITASK
                await UniTask.SwitchToMainThread();
#elif UNITY_2023_1_OR_NEWER
                await Awaitable.MainThreadAsync();
#else
                // Task 환경이면 이미 메인 스레드에서만 호출한다고 가정
#endif

                var item = await _factory.CreateAsync(ct);
                CreatedCount++;
                AttachOwner(item, this); // BackRef 부착

                _onBeforeReturn?.Invoke(item);

                lock (_sync)
                {
                    ThrowIfDisposed();
                    _free.Enqueue(item);
                }

                InvokeOnReturn(item);
            }
        }
        

        // -------- Rent / Return --------

        /// <summary>
        /// 즉시 대여를 시도한다. 없으면 실패한다.
        /// </summary>
        public bool TryRent(out T item)
        {
            ThrowIfDisposed();

            lock (_sync)
            {
                if (_free.Count > 0)
                {
                    item = _free.Dequeue();
                    _inUse.Add(item);
                }
                else { item = null; }
            }

            if (!item) return false;

            InvokeOnRent(item);
            return true;
        }

        /// <summary>
        /// 객체를 대여한다.
        /// - Free에 있으면 즉시 반환
        /// - 없으면 새로 생성
        /// Capacity는 반환 시 정리(축소)에만 사용한다.
        /// </summary>
        public async Awaitable<T> Rent(CancellationToken ct = default)
        {
            MainThreadGuard.AssertMainThread();

            if (TryRent(out var fromFree))
            {
                _onAfterRent?.Invoke(fromFree);
                return fromFree;
            }
            

#if AESFW_UNITASK
            await UniTask.SwitchToMainThread();
#elif UNITY_2023_1_OR_NEWER
            await Awaitable.MainThreadAsync();
#else
            // Task 환경이면 이미 메인 스레드에서만 호출한다고 가정
#endif

            var created = await _factory.CreateAsync(ct);
            CreatedCount++;
            AttachOwner(created, this);

            lock (_sync)
            {
                ThrowIfDisposed();
                _inUse.Add(created);
            }

            InvokeOnRent(created);
            _onAfterRent?.Invoke(created);
            return created;
        }

        /// <summary>
        /// 객체를 반환한다.
        /// Capacity를 넘는 여유 객체는 파괴한다.
        /// </summary>
        public void Return(T item)
        {
            if (!item) return;
            MainThreadGuard.AssertMainThread();

            _onBeforeReturn?.Invoke(item);

            bool evict = false;
            T evicted = null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool doubleReturn = false;
            bool crossPool = false;
            var br = GetBackRef(item);
            if (br && br.OwnerPool != this) crossPool = true;
#endif

            lock (_sync)
            {
                if (_disposed)
                {
                    // 이미 풀 해제된 상태면 그냥 파괴
                    evict = true;
                    evicted = item;
                    goto AFTER_LOCK;
                }

                if (!_inUse.Remove(item))
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    doubleReturn = true;
#endif
                    goto AFTER_LOCK;
                }

                _free.Enqueue(item);

                // Capacity 초과 시 Free에서 제거
                if (_free.Count + _inUse.Count > Capacity && _free.Count > 0)
                {
                    evict = true;
                    evicted = _free.Dequeue();
                }
            }

            AFTER_LOCK:

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (crossPool)
                Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Cross-pool return: {item}");

            if (doubleReturn)
                Debug.LogWarning($"[ObjectPool<{typeof(T).Name}>] Double return: {item}");
#endif

            // 풀에 남아 있는 대상에 대해 OnReturn 호출
            InvokeOnReturn(item);

            if (evict && evicted) { DestroyOnMain(evicted); }
        }

        /// <summary>
        /// `GameObject`를 풀로 반환한다.
        /// </summary>
        void IGameObjectPool.Return(GameObject go)
        {
            if (!go) return;

            if (typeof(T) == typeof(GameObject))
            {
                Return((T)(Object)go);
                return;
            }

            var comp = go.GetComponent<T>();
            if (comp) Return(comp);
            else DestroyOnMain(go);
        }

        // -------- BackRef / IPoolable 호출 --------

        private static void AttachOwner(T item, IGameObjectPool owner)
        {
            if (!item) return;

            switch (item)
            {
                case GameObject go:
                    go.GetOrAdd<PoolBackRef>().SetOwner(owner);
                    break;
                case Component c:
                    c.gameObject.GetOrAdd<PoolBackRef>().SetOwner(owner);
                    break;
            }
        }

        private static PoolBackRef GetBackRef(T item)
        {
            return item switch
            {
                GameObject go => go.GetComponent<PoolBackRef>(),
                Component c => c.gameObject.GetComponent<PoolBackRef>(),
                _ => null
            };
        }

        private static IPoolable[] GetCachedPoolables(T item)
        {
            switch (item)
            {
                case GameObject go:
                    return go.GetComponent<PoolBackRef>()?.GetPoolables();
                case Component c:
                    return c.gameObject.GetComponent<PoolBackRef>()?.GetPoolables();
                default:
                    return null;
            }
        }

        private static void InvokeOnRent(T item)
        {
            var arr = GetCachedPoolables(item);
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++) arr[i].OnRent();
        }

        private static void InvokeOnReturn(T item)
        {
            var arr = GetCachedPoolables(item);
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++) arr[i].OnReturn();
        }

        // 메인 스레드 파괴 (fire-and-forget)
        private void DestroyOnMain(Object obj)
        {
            _ = DestroyOnMainAsync(obj);
        }

        private async UniTaskVoid DestroyOnMainAsync(Object obj)
        {
#if AESFW_UNITASK
            await UniTask.SwitchToMainThread();
#elif UNITY_2023_1_OR_NEWER
            await Awaitable.MainThreadAsync();
#else
            // Task 환경이면 별도의 메인쓰레드 디스패처가 있다면 여기서 사용
            // 지금은 이미 메인에서만 호출된다고 가정
#endif
            _factory.Destroy((T)obj);
            DestroyedCount++;
        }

        // -------- Dispose --------

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Queue<T> free;
            List<T> inUse;

            lock (_sync)
            {
                free = new Queue<T>(_free);
                _free.Clear();
                inUse = new List<T>(_inUse);
                _inUse.Clear();
            }

            while (free.Count > 0) DestroyOnMain(free.Dequeue());
            foreach (var it in inUse) DestroyOnMain(it);

        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException($"ObjectPool<{typeof(T).Name}>");
        }

#if UNITY_EDITOR
        /// <summary>디버그용: 사용 중 컬렉션 스냅샷</summary>
        internal IReadOnlyCollection<T> Debug_InUse
        {
            get
            {
                lock (_sync) return new List<T>(_inUse);
            }
        }

        /// <summary>
        /// 누수 리포트를 출력한다.
        /// </summary>
        public void LeakReport()
        {
            lock (_sync) { PoolLeakDetector.LeakReport($"ObjectPool<{typeof(T).Name}>", _inUse); }
        }
#endif
    }
}