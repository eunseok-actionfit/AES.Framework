using DG.Tweening;
using UnityEngine;


namespace AES.Tools
{
    public sealed class TransformLocalRotationZBinding : ContextBindingBase
    {
        private System.Action<object> _listener;
        private object _token;

        [Header("Tween")]
        public bool useTween = false;

        [AesShowIf("useTween")]
        [Min(0f)]
        public float tweenDuration = 0.25f;

        [AesShowIf("@useTween && !useAnimationCurve")]
        [AesEnumToggleButtons]
        public Ease tweenEase = Ease.Linear;

        [AesShowIf("useTween")]
        public bool useAnimationCurve = false;

        [AesShowIf("@useTween && useAnimationCurve")]
        public AnimationCurve tweenCurve;

        [Header("Gizmo")]
        public bool drawGizmo = true;
        public float gizmoLength = 0.5f;

        private Tween _activeTween;

        // 디버그용: 마지막 목표 각도
        private bool _hasTarget;
        private float _lastTargetAngle;

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChanged;
            _token = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _token);

            _listener = null;
            _token = null;

            _activeTween?.Kill();
            _activeTween = null;

            _hasTarget = false;
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            if (!(value is float angle))
                return;

            _hasTarget = true;
            _lastTargetAngle = angle;

            Vector3 euler = transform.localEulerAngles;
            Vector3 target = new Vector3(euler.x, euler.y, angle);

            if (!useTween)
            {
                _activeTween?.Kill();
                transform.localEulerAngles = target;
                return;
            }

            _activeTween?.Kill();

            Tween tween = transform.DOLocalRotate(target, tweenDuration, RotateMode.Fast);

            if (useAnimationCurve && tweenCurve != null && tweenCurve.keys.Length > 0)
                tween.SetEase(tweenCurve);
            else
                tween.SetEase(tweenEase);

            _activeTween = tween;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawGizmo) return;

            Vector3 pos = transform.position;

            // 현재 로컬 Z 회전 방향
            Gizmos.color = Color.green;
            Vector3 currentDir = transform.right;
            Gizmos.DrawLine(pos, pos + currentDir * gizmoLength);

            // 목표 Z 회전 방향
            if (_hasTarget)
            {
                Gizmos.color = Color.yellow;
                Quaternion targetRot = Quaternion.Euler(0f, 0f, _lastTargetAngle);
                Vector3 targetDir = targetRot * Vector3.right;
                Gizmos.DrawLine(pos, pos + targetDir * gizmoLength);
            }
        }
#endif
    }
}
