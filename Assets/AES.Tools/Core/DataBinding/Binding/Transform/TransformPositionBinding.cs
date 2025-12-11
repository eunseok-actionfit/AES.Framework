using System;
using UnityEngine;
using DG.Tweening;
using AES.Tools.Editor;

namespace AES.Tools
{
    public sealed class TransformPositionBinding : ContextBindingBase
    {
        private Action<object> _listener;
        private object _token;

        [Header("Space")]
        [AesEnumToggleButtons]
        public PositionBindingSpace space = PositionBindingSpace.World;

        [Header("Tween")]
        public bool useTween = false;

        [AesShowIf("useTween")]
        [AesEnumToggleButtons]
        public TweenApplyMode tweenMode = TweenApplyMode.Ease;

        [AesShowIf("useTween")]
        [Min(0f)]
        public float tweenDuration = 0.25f;

        [AesShowIf("@useTween && tweenMode == TweenApplyMode.Ease")]
        public Ease tweenEase = Ease.Linear;

        [AesShowIf("@useTween && tweenMode == TweenApplyMode.Curve")]
        public AnimationCurve tweenCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Distance Warp")]
        [AesLabelText("멀리 떨어져 있으면 워프")]
        public bool useDistanceWarp = false;

        [AesShowIf("useDistanceWarp")]
        [Min(0f)]
        [AesLabelText("워프로 처리할 최소 거리")]
        public float warpDistance = 10f;

        private Tween _activeTween;
        private bool _hasReceivedValue; 

        // 더 이상 Awake에서 화면 밖으로 보내지 않음

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChanged;
            _token    = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (_token is IDisposable d)
                d.Dispose();
            else if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _token);

            _listener = null;
            _token    = null;

            _activeTween?.Kill();
            _activeTween = null;
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            bool isWorld = (space == PositionBindingSpace.World);
            Vector3 current = isWorld ? transform.position : transform.localPosition;

            Vector3 target;
            if (value is Vector3 v3)
                target = v3;
            else if (value is Vector2 v2)
                target = new Vector3(v2.x, v2.y, current.z);
            else
                return;

            // 첫 값은 무조건 워프 (즉시 적용)
            if (!_hasReceivedValue)
            {
                _hasReceivedValue = true;
                _activeTween?.Kill();

                if (isWorld)
                    transform.position = target;
                else
                    transform.localPosition = target;

                return;
            }

            // 거리 기반 워프 판단
            bool shouldWarp = false;
            if (useDistanceWarp && warpDistance > 0f)
            {
                float sqrDist = (target - current).sqrMagnitude;
                float warpDistSqr = warpDistance * warpDistance;
                if (sqrDist >= warpDistSqr)
                    shouldWarp = true;
            }

            // 트윈 안 쓰거나, duration 0, 혹은 거리 워프 조건이면 즉시 적용
            if (!useTween || tweenDuration <= 0f || shouldWarp)
            {
                _activeTween?.Kill();

                if (isWorld)
                    transform.position = target;
                else
                    transform.localPosition = target;

                return;
            }

            // 트윈 사용
            _activeTween?.Kill();

            Tween tween = isWorld
                ? transform.DOMove(target, tweenDuration)
                : transform.DOLocalMove(target, tweenDuration);

            if (tweenMode == TweenApplyMode.Curve)
                tween.SetEase(tweenCurve);
            else
                tween.SetEase(tweenEase);

            _activeTween = tween;
        }
    }
}
