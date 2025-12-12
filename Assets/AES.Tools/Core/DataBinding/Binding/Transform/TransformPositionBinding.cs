using System;
using UnityEngine;
using UnityEngine.Events;
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

        [Header("Events")]
        public UnityEvent OnReachedTarget;

        [Min(0f)]
        public float reachedEpsilon = 0.01f;

        [Header("Duplicate Target Filter")]
        [Min(0f)]
        [AesLabelText("같은 타겟으로 간주할 거리(이 값 이내면 같은 타겟)")]
        public float sameTargetEpsilon = 0.0005f;

        private Tween _activeTween;

        private bool _hasReceivedValue;
        private bool _hasLastTarget;
        private Vector3 _lastTarget;
        private bool _reachedFiredForCurrentTarget;

        private int _changeSeq = 0;

        // --- Gizmo (Debug) ---
        [Header("Gizmo")]
        public bool drawGizmo = true;

        [AesShowIf("drawGizmo")]
        [Min(0f)]
        public float gizmoRadius = 0.08f;

        [AesShowIf("drawGizmo")]
        [Min(0f)]
        public float gizmoEpsilonRadius = 0.15f;

        [AesShowIf("drawGizmo")]
        [Min(0f)]
        public float gizmoLineHeight = 0f; // 2D면 0, 3D면 살짝 띄우고 싶을 때

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChanged;
            _token = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            UnregisterListenerSafe();
            ResetRuntimeState();
        }

        private void UnregisterListenerSafe()
        {
            try
            {
                if (_token is IDisposable d)
                {
                    d.Dispose();
                }
                else if (BindingContext != null && _listener != null)
                {
                    BindingContext.RemoveListener(ResolvedPath, _token);
                }
            }
            finally
            {
                _listener = null;
                _token = null;
            }
        }

        private void ResetRuntimeState()
        {
            KillActiveTween();
            _hasReceivedValue = false;
            _hasLastTarget = false;
            _reachedFiredForCurrentTarget = false;
            _changeSeq = 0;
        }

        private void KillActiveTween()
        {
            if (_activeTween == null) return;
            _activeTween.Kill(false);
            _activeTween = null;
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            _changeSeq++;

            bool isWorld = (space == PositionBindingSpace.World);

            // 결정성: 먼저 트윈 정리 후 current 읽기
            KillActiveTween();

            Vector3 current = GetCurrentPosition(isWorld);

            if (!TryParseTarget(value, current, out Vector3 target))
                return;

            // 같은 타겟이어도 "이미 current가 도착"일 때만 무시
            if (_hasLastTarget && IsWithinEpsilon(target, _lastTarget, sameTargetEpsilon))
            {
                if (IsReached(current, target))
                    return;
            }

            _hasLastTarget = true;
            _lastTarget = target;
            _reachedFiredForCurrentTarget = false;

            // 첫 수신: 즉시 적용
            if (!_hasReceivedValue)
            {
                _hasReceivedValue = true;
                ApplyPosition(isWorld, target);
                TryInvokeReachedOnce(isWorld, target);
                return;
            }

            // 이미 도착이면 reached만(1회)
            if (IsReached(current, target))
            {
                TryInvokeReachedOnce(isWorld, target);
                return;
            }

            bool shouldWarp = ShouldWarp(current, target);

            // 즉시 적용(트윈 미사용/0초/워프)
            if (!useTween || tweenDuration <= 0f || shouldWarp)
            {
                ApplyPosition(isWorld, target);
                TryInvokeReachedOnce(isWorld, target);
                return;
            }

            // 트윈 시작
            StartMoveTween(isWorld, target);
        }

        private bool TryParseTarget(object value, Vector3 current, out Vector3 target)
        {
            if (value is Vector3 v3)
            {
                target = v3;
                return true;
            }

            if (value is Vector2 v2)
            {
                // keepZ는 "현재 위치의 Z" 유지
                target = new Vector3(v2.x, v2.y, current.z);
                return true;
            }

            target = default;
            return false;
        }

        private Vector3 GetCurrentPosition(bool isWorld)
            => isWorld ? transform.position : transform.localPosition;

        private void ApplyPosition(bool isWorld, Vector3 target)
        {
            if (isWorld) transform.position = target;
            else transform.localPosition = target;
        }

        private bool ShouldWarp(Vector3 current, Vector3 target)
        {
            if (!useDistanceWarp) return false;
            if (warpDistance <= 0f) return false;

            float sqrDist = (target - current).sqrMagnitude;
            float warpSqr = warpDistance * warpDistance;
            return sqrDist >= warpSqr;
        }

        private bool IsReached(Vector3 current, Vector3 target)
        {
            float eps = Mathf.Max(0f, reachedEpsilon);
            return (target - current).sqrMagnitude <= eps * eps;
        }

        private static bool IsWithinEpsilon(Vector3 a, Vector3 b, float eps)
        {
            float e = Mathf.Max(0f, eps);
            return (a - b).sqrMagnitude <= e * e;
        }

        private void StartMoveTween(bool isWorld, Vector3 target)
        {
            Tween tween = isWorld
                ? transform.DOMove(target, tweenDuration)
                : transform.DOLocalMove(target, tweenDuration);

            if (tweenMode == TweenApplyMode.Curve) tween.SetEase(tweenCurve);
            else tween.SetEase(tweenEase);

            _activeTween = tween
                .OnUpdate(() => TryInvokeReachedOnce(isWorld, target))
                .OnComplete(() => TryInvokeReachedOnce(isWorld, target));
        }

        private void TryInvokeReachedOnce(bool isWorld, Vector3 target)
        {
            if (_reachedFiredForCurrentTarget) return;
            if (OnReachedTarget == null) return;

            Vector3 cur = GetCurrentPosition(isWorld);
            if (IsReached(cur, target))
            {
                _reachedFiredForCurrentTarget = true;
                OnReachedTarget.Invoke();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGizmo) return;

            bool isWorld = (space == PositionBindingSpace.World);

            Vector3 cur = isWorld ? transform.position : transform.localPosition;
            Vector3 originWorld = transform.position;
            Vector3 offset = new Vector3(0f, gizmoLineHeight, 0f);

            // 현재 위치(초록)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(originWorld + offset, gizmoRadius);

            // 목표 타겟(노랑) + reached 범위(반투명)
            if (_hasLastTarget)
            {
                Vector3 targetWorld = isWorld ? _lastTarget : transform.parent ? transform.parent.TransformPoint(_lastTarget) : _lastTarget;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(targetWorld + offset, gizmoRadius);

                // reached epsilon 범위(청록)
                float eps = Mathf.Max(0f, reachedEpsilon);
                if (eps > 0f)
                {
                    Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
                    Gizmos.DrawSphere(targetWorld + offset, Mathf.Max(eps, gizmoEpsilonRadius * 0.25f));

                    Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
                    Gizmos.DrawWireSphere(targetWorld + offset, eps);
                }

                // 현재->타겟 라인
                Gizmos.color = Color.white;
                Gizmos.DrawLine(originWorld + offset, targetWorld + offset);
            }

            // 같은 타겟 eps(자홍): lastTarget 기준 “같다고 판단하는 반경”
            if (_hasLastTarget && sameTargetEpsilon > 0f)
            {
                Vector3 targetWorld = isWorld ? _lastTarget : transform.parent ? transform.parent.TransformPoint(_lastTarget) : _lastTarget;
                Gizmos.color = new Color(1f, 0f, 1f, 0.6f);
                Gizmos.DrawWireSphere(targetWorld + offset, sameTargetEpsilon);
            }
        }
#endif
    }
}
