using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.View
{
    [RequireComponent(typeof(UIViewHints))]
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIView : MonoBehaviour, IUIView, IPoolable
    {
        [Header("CanvasGroup")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [Tooltip("Show 중에 raycast 허용 여부")]
        [SerializeField] protected bool blocksRaycasts = true;

        [Header("Transition")]
        [SerializeField] protected TransitionAsset overrideTransition; // null이면 즉시
        [Header("Ordering")]
        [SerializeField] private int zPriority;
        public int ZPriority => zPriority;

        public CanvasGroup CanvasGroup => canvasGroup;
        public RectTransform Rect => transform as RectTransform;

        // 상태
        public bool IsShown { get; private set; }
        public bool IsTransitioning { get; private set; }

        // 재진입/중복 방지
        private readonly AsyncLock _transitionLock = new();


        protected void Reset() => canvasGroup = GetComponent<CanvasGroup>();
        
        private CancellationToken _destroyToken;
        public CancellationToken DestroyToken => _destroyToken;

        protected virtual void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            _destroyToken = this.GetCancellationTokenOnDestroy();
        }

        // ---- 확장 훅 (선택) ----
        protected virtual UniTask OnBeforeShow(CancellationToken ct) => UniTask.CompletedTask;
        protected virtual UniTask OnAfterShow(CancellationToken ct) => UniTask.CompletedTask;
        protected virtual UniTask OnBeforeHide(CancellationToken ct) => UniTask.CompletedTask;
        protected virtual UniTask OnAfterHide(CancellationToken ct) => UniTask.CompletedTask;


        protected virtual UniTask OnShow(object model, CancellationToken ct) => UniTask.CompletedTask;
        protected virtual UniTask OnHide(CancellationToken ct) => UniTask.CompletedTask;

        // ---- 편의 오버로드 ----
        public UniTask ShowAsync(object model, CancellationToken ct = default) =>
            ShowAsync(model, overrideTransition, ct);

        public UniTask HideAsync(CancellationToken ct = default) =>
            HideAsync(overrideTransition, ct);

        public async UniTask UpdateModelAsync(object model, CancellationToken ct = default)
        {
            await OnShow(model, ct);
        }

        // ---- 표준 Show/Hide ----
        public async UniTask ShowAsync(object model, IUITransition transition, CancellationToken ct)
        {
            using (await _transitionLock.LockAsync(ct))
            {
                if (ct.IsCancellationRequested || !this || !canvasGroup)
                    return;

                IsTransitioning = true;
                try
                {
                    gameObject.SetActive(true);
                    await OnBeforeShow(ct);

                    // 중간에 파괴/취소 여부 재확인 (선택 사항이지만 방어적으로 좋음)
                    if (ct.IsCancellationRequested || !this || !canvasGroup)
                        return;

                    await OnShow(model, ct);
                    canvasGroup.blocksRaycasts = blocksRaycasts;

                    var t = overrideTransition ?? transition;
                    canvasGroup.alpha = 1f;

                    if (t != null)
                    {
                        try
                        {
                            await t.In(this, ct);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    if (ct.IsCancellationRequested || !this || !canvasGroup)
                        return;

                    IsShown = true;
                    await OnAfterShow(ct);
                }
                finally { IsTransitioning = false; }
            }
        }



        public async UniTask HideAsync(IUITransition transition, CancellationToken ct)
        {
            using (await _transitionLock.LockAsync(ct))
            {
                if (ct.IsCancellationRequested || !this || !canvasGroup)
                    return;

                IsTransitioning = true;
                try
                {
                    canvasGroup.blocksRaycasts = false;
                    await OnBeforeHide(ct);

                    if (ct.IsCancellationRequested || !this || !canvasGroup)
                        return;

                    var t = overrideTransition ?? transition;
                    if (t != null)
                    {
                        try
                        {
                            await t.Out(this, ct);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    if (ct.IsCancellationRequested || !this || !canvasGroup)
                        return;

                    canvasGroup.alpha = 0f;

                    await OnHide(ct);

                    if (!this) // 파괴되었으면 gameObject 접근 금지
                        return;

                    gameObject.SetActive(false);
                    IsShown = false;
                    await OnAfterHide(ct);
                }
                finally { IsTransitioning = false; }
            }
        }


        public virtual void OnRent()
        {
            if (CanvasGroup) CanvasGroup.alpha = 0f;
        }

        public virtual void OnReturn()
        {
            if (CanvasGroup) {
                CanvasGroup.alpha = 0f;
                CanvasGroup.blocksRaycasts = false;
            }
        }
    }

    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIView<TViewModel> : UIView
    {
        protected UguiBinder Binder { get; private set; }
        public TViewModel Model { get; private set; } // 현재 모델 캐시

        protected override void Awake()
        {
            base.Awake();
            Binder = new UguiBinder(this);
        }

        /// <summary>모델 변경 시 바인딩. 한 번만 보장하지 않으니 idempotent하게 작성 권장.</summary>
        protected abstract void Bind(TViewModel model);

        /// <summary>모델 교체 시, 필요한 최소 갱신을 하고 싶으면 오버라이드.</summary>
        protected virtual void Rebind(TViewModel model) => Bind(model);

        protected override async UniTask OnShow(object model, CancellationToken ct)
        {
            if (model is not TViewModel typed)
                throw new ArgumentException($"UIView<{typeof(TViewModel).Name}> requires model of type {typeof(TViewModel).Name}, but got {model?.GetType().Name ?? "null"}");

            // 최초/교체 판정
            bool first = Equals(Model, default(TViewModel));
            bool replaced = !first && !ReferenceEquals(Model, typed);

            Model = typed;

            if (first) Bind(Model);
            else if (replaced) Rebind(Model);

            await UniTask.CompletedTask;
        }

        // Hide 시 모델 캐시 유지/삭제는 정책에 따라 선택.
        protected override UniTask OnHide(CancellationToken ct)
        {
            // 필요하다면 여기서 Model = default; 등 정리.
            return UniTask.CompletedTask;
        }
    }

    internal sealed class AsyncLock
    {
        private readonly SemaphoreSlim _sem = new(1, 1);

        public async UniTask<IDisposable> LockAsync(CancellationToken ct)
        {
            await _sem.WaitAsync(ct);
            return new Releaser(_sem);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _s;
            public Releaser(SemaphoreSlim s) => _s = s;

            public void Dispose()
            {
                try { _s.Release(); }
                catch { // ignored
                }
            }
        }
    }
}