// 3) TransformLocalRotationZBinding.cs (2D 회전용)
using UnityEngine;
using DG.Tweening;
using AES.Tools.Editor;

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

        private Tween _activeTween;

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _listener = OnValueChanged;
            _token    = context.RegisterListener(path, _listener);
        }

        protected override void OnContextUnavailable()
        {
            if (BindingContext != null && _listener != null)
                BindingContext.RemoveListener(ResolvedPath, _token);

            _activeTween?.Kill();
            _activeTween = null;
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_OnValueUpdated(value, ResolvedPath);
#endif
            if (!(value is float angle))
                return;

            var euler = transform.localEulerAngles;
            Vector3 target = new Vector3(euler.x, euler.y, angle);

            if (!useTween)
            {
                _activeTween?.Kill();
                transform.localEulerAngles = target;
                return;
            }

            _activeTween?.Kill();

            var tween = transform.DOLocalRotate(target, tweenDuration, RotateMode.Fast);

            if (useAnimationCurve && tweenCurve != null && tweenCurve.keys.Length > 0)
                tween.SetEase(tweenCurve);
            else
                tween.SetEase(tweenEase);

            _activeTween = tween;
        }
    }
}
