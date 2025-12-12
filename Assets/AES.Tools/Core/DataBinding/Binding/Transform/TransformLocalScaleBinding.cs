// 4) TransformLocalScaleBinding.cs
using UnityEngine;
using DG.Tweening;


namespace AES.Tools
{
    public sealed class TransformLocalScaleBinding : ContextBindingBase
    {
        private System.Action<object> _listener;
        private object _token;

        [Header("Tween")]
        public bool useTween = false;

        [AesShowIf("useTween")]
        [Min(0f)]
        public float tweenDuration = 0.25f;

        [AesShowIf("@useTween && !useAnimationCurve")]
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
            Vector3 target;

            if (value is Vector3 v3)
                target = v3;
            else if (value is float s)
                target = new Vector3(s, s, 1f);
            else
                return;

            if (!useTween)
            {
                _activeTween?.Kill();
                transform.localScale = target;
                return;
            }

            _activeTween?.Kill();

            var tween = transform.DOScale(target, tweenDuration);

            if (useAnimationCurve && tweenCurve != null && tweenCurve.keys.Length > 0)
                tween.SetEase(tweenCurve);
            else
                tween.SetEase(tweenEase);

            _activeTween = tween;
        }
    }
}
