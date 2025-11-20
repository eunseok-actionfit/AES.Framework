using System;
using AES.Tools.Components.Binding;
using AES.Tools.Core;
using AES.Tools.Guards;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace AES.Tools.Components.Buttons
{
    public sealed class UIButtonWidget : Button
    {
        [Header("Input")]
        [SerializeField, Min(0f)]
        private float throttleSeconds = 0.4f;


        [Header("Unity Events - Feedback / Animations")]
        [SerializeField]
        private UnityEvent onPressed;
        [SerializeField]
        private UnityEvent onDenied;
        [SerializeField]
        private UnityEvent onAttentionPing;

        [Header("Unity Events - State")]
        [SerializeField]
        private UnityEvent onEnabledVisual;
        [SerializeField]
        private UnityEvent onDisabledVisual;
        [SerializeField]
        private UnityEvent onIdleStop;

        private IInputGuard _guard;
        private IUiLockService _uiLock;

        private Func<UniTask> _command;
        private string _btnId;
        private bool _inFlight;

        private IDisposable _enabledSub;

        protected override void Awake()
        {
            base.Awake();

            _btnId = $"btn_{gameObject.GetInstanceID()}";

            // DI 대신 전역 서비스에서 주입
            _guard ??= UiServices.InputGuard;
            _uiLock ??= UiServices.UiLock;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            onClick.AddListener(OnClicked);

            // 상태에 맞게 비주얼 이벤트 호출
            if (interactable) onEnabledVisual?.Invoke();
            else onDisabledVisual?.Invoke();
        }

        protected override void OnDisable()
        {
            onClick.RemoveListener(OnClicked);

            _enabledSub?.Dispose();
            _enabledSub = null;

            // idle 종료 이벤트
            onIdleStop?.Invoke();

            base.OnDisable();
        }

        public void Initialize(bool isEnabled, Func<UniTask> command)
        {
            _enabledSub?.Dispose();
            _enabledSub = null;

            _command = command;
            SetEnabled(isEnabled);
        }

        public void Initialize(ButtonVM vm)
        {
            if (vm == null) throw new ArgumentNullException(nameof(vm));

            _enabledSub?.Dispose();
            _enabledSub = null;

            _command = vm.Command;

            if (vm.IsEnabled != null)
                _enabledSub = vm.IsEnabled.SubscribeAndReplay(SetEnabled, refreshNow: true);
            else
                SetEnabled(true);
        }

        private void SetEnabled(bool enabled)
        {
            if (_inFlight) return;

            interactable = enabled;

            if (enabled) onEnabledVisual?.Invoke();
            else onDisabledVisual?.Invoke();
        }

        private void OnClicked() => RunAsync().Forget();

        private async UniTaskVoid RunAsync()
        {
            if (_inFlight || _command == null) return;

            var destroyToken = this.GetCancellationTokenOnDestroy();

            // Throttle 체크
            if (throttleSeconds > 0f &&
                !(_guard?.Throttle(_btnId, throttleSeconds) ?? true))
            {
                onDenied?.Invoke();
                onAttentionPing?.Invoke();
                return;
            }

            _inFlight = true;

            var btn = (Button)this;
            bool prevInteractable = btn && btn.interactable;
            if (btn) btn.interactable = false;

            // idle 종료
            onIdleStop?.Invoke();

            try
            {
                using (_uiLock?.ScopedLock())
                {

                    onPressed?.Invoke();

                    await _command().AttachExternalCancellation(destroyToken);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                _inFlight = false;

                if (btn)
                    btn.interactable = prevInteractable;

                // 상태에 따라 다시 비주얼 갱신
                if (btn && btn.interactable) onEnabledVisual?.Invoke();
                else onDisabledVisual?.Invoke();
            }
        }
    }
}