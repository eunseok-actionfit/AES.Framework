using System;
using System.Collections.Generic;
using System.Threading;
using AES.Tools.View;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AES.Tools
{
    /// <summary>
    /// 동적으로 생성되는 Popup 전용 매니저.
    /// - Enqueue 기반 큐잉
    /// - 현재 열린 팝업이 닫히면 큐의 다음 팝업 자동 표시
    /// - 열린 팝업은 Stack 으로 관리 (BackKey / CloseTop 등)
    /// - Prefab 은 UIRegistry(UIRegistryBase) + IUIFactory 를 통해 생성
    /// </summary>
    public sealed class PopupManager : MonoBehaviour
    {
        [Header("Popup Root (씬에 하나)")]
        [SerializeField] Transform popupRoot;

        IUIFactory     _factory = new UIFactory();
        [SerializeField] UIRegistrySO registry;

        readonly Queue<PopupRequestBase> _queue = new();
        readonly Stack<ActivePopup>      _stack = new();

        bool _isShowing;
        
        void Awake()
        {
            if (popupRoot == null)
                Debug.LogError("[PopupManager] popupRoot 가 설정되지 않았습니다.", this);
        }

        // =========================================================
        //  Public API
        // =========================================================

        /// <summary>
        /// 팝업을 큐에 등록하고, 실제 화면에 표시될 때 인스턴스를 반환한다.
        /// - 큐에만 쌓이고, 현재 떠 있는 팝업이 없으면 바로 Show
        /// - 호출측은 await 해서 실제 View 인스턴스가 생성된 시점에 셋업 가능
        /// </summary>
        public UniTask<TView> EnqueueAsync<TView>(
            object viewModel = null,
            CancellationToken ct = default)
            where TView : PopupViewBase
        {
            var tcs  = new UniTaskCompletionSource<TView>();
            var item = new PopupRequest<TView>(viewModel, tcs);

            _queue.Enqueue(item);

            // 현재 열린 팝업이 없으면 바로 다음 팝업 시도
            _ = TryShowNextAsync(ct);

            return tcs.Task;
        }

        /// <summary>
        /// 큐를 통하지 않고 즉시 현재 스택 위에 팝업을 띄운다.
        /// (이미 떠 있는 팝업 위로 Confirm 같은 걸 덮어쓰고 싶을 때)
        /// </summary>
        public async UniTask<TView> PushImmediateAsync<TView>(
            object viewModel = null,
            CancellationToken ct = default)
            where TView : PopupViewBase
        {
            var popup = await CreatePopupViewAsync<TView>(viewModel, ct);

            popup.Show(); 

            var entry = GetEntryForViewType(typeof(TView));

            _stack.Push(new ActivePopup(entry, popup));

            return popup;
        }

        /// <summary>
        /// 스택 최상단 팝업을 닫는다 (BackKey 등에서 사용).
        /// 실제 닫힘 애니메이션 완료 시점에는 OnPopupClosed 를 호출해줘야 한다.
        /// </summary>
        public void CloseTop()
        {
            if (_stack.Count == 0)
                return;

            var top = _stack.Peek();
            if (top.View == null)
                return;

            top.View.Hide(); // Hide 트리거만, 실제 완료는 View 쪽에서 처리
        }

        /// <summary>
        /// 모든 팝업을 즉시 정리 (큐 + 스택 전부).
        /// </summary>
        public void ClearAll()
        {
            _queue.Clear();

            while (_stack.Count > 0)
            {
                var active = _stack.Pop();
                if (active.View != null)
                    _factory.Destroy(active.Entry, active.View);
            }
        }

        /// <summary>
        /// PopupView/UIView 쪽에서 "완전히 닫힘" 시점에 호출해줄 진입점.
        /// - UnityEvent(OnClosed) 에 이 메서드를 바인딩해두면 된다.
        /// </summary>
        public void OnPopupClosed(PopupViewBase closedView)
        {
            if (closedView == null)
                return;

            if (_stack.Count == 0)
                return;

            var top = _stack.Peek();
            if (top.View != closedView)
                return; // 내가 관리하던 top 이 아니면 무시

            _stack.Pop();
            _factory.Destroy(top.Entry, top.View);

            // 닫힌 뒤 다음 큐 시도
            _ = TryShowNextAsync();
        }

        // =========================================================
        //  내부 로직
        // =========================================================

        async UniTask TryShowNextAsync(CancellationToken ct = default)
        {
            if (_isShowing)
                return;

            if (_stack.Count > 0)
                return;

            if (_queue.Count == 0)
                return;

            _isShowing = true;
            try
            {
                var req = _queue.Dequeue();

                var (entry, view) = await req.CreateAndShowAsync(this, ct);

                _stack.Push(new ActivePopup(entry, view));
            }
            finally
            {
                _isShowing = false;
            }
        }

        internal async UniTask<TView> CreatePopupViewAsync<TView>(
            object viewModel,
            CancellationToken ct)
            where TView : PopupViewBase
        {
            if (popupRoot == null)
                throw new InvalidOperationException("popupRoot is not set on PopupManager.");

            var viewType = typeof(TView);
            var entry    = GetEntryForViewType(viewType);

            var uiView = await _factory.CreateAsync(entry, popupRoot, ct);

            var popup = uiView.GetComponent<TView>();
            if (!popup)
            {
                throw new MissingComponentException(
                    $"Popup prefab for {viewType.Name} is missing component {viewType.Name}.");
            }

            // ViewModel 바인딩
            popup.BindModelObject(viewModel);

            return popup;
        }

        UIRegistryEntry GetEntryForViewType(Type viewType)
        {
            if (!registry.TryGetByViewType(viewType, out var entry) || entry == null)
            {
                throw new InvalidOperationException(
                    $"UIPopupRegistry 에서 View 타입 {viewType.Name} 에 해당하는 UIRegistryEntry 를 찾지 못했습니다.");
            }

            return entry;
        }

        // =========================================================
        //  내부 helper 구조체/클래스
        // =========================================================

        readonly struct ActivePopup
        {
            public readonly UIRegistryEntry Entry;
            public readonly PopupViewBase   View;

            public ActivePopup(UIRegistryEntry entry, PopupViewBase view)
            {
                Entry = entry;
                View  = view;
            }
        }

        abstract class PopupRequestBase
        {
            public abstract UniTask<(UIRegistryEntry entry, PopupViewBase view)>
                CreateAndShowAsync(PopupManager manager, CancellationToken ct);
        }

        sealed class PopupRequest<TView> : PopupRequestBase
            where TView : PopupViewBase
        {
            readonly object _viewModel;
            readonly UniTaskCompletionSource<TView> _tcs;

            public PopupRequest(object viewModel, UniTaskCompletionSource<TView> tcs)
            {
                _viewModel = viewModel;
                _tcs       = tcs;
            }

            public override async UniTask<(UIRegistryEntry entry, PopupViewBase view)>
                CreateAndShowAsync(PopupManager manager, CancellationToken ct)
            {
                var popup = await manager.CreatePopupViewAsync<TView>(_viewModel, ct);

                popup.Show();

                var entry = manager.GetEntryForViewType(typeof(TView));

                _tcs.TrySetResult(popup);

                return (entry, popup);
            }
        }
    }
}
