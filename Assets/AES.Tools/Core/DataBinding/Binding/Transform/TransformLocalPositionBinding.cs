// 2) TransformLocalPositionBinding.cs
using System;
using DG.Tweening;
using UnityEngine;


namespace AES.Tools
{
    public sealed class TransformLocalPositionBinding : ContextBindingBase
    {
        private Action<object> _listener;
        private object         _token;

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
        public AnimationCurve tweenCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private Tween _activeTween;

        // 시작 시 보이지 않게
        void Awake()
        {
            transform.localPosition = new Vector3(99999, 99999, 0); // 화면 밖
        }
        
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
            Vector3 target;

            if (value is Vector3 v3)
                target = v3;
            else if (value is Vector2 v2)
                target = new Vector3(v2.x, v2.y, transform.localPosition.z);
            else
                return;

            if (!useTween)
            {
                _activeTween?.Kill();
                transform.localPosition = target;
                return;
            }

            _activeTween?.Kill();

            var tween = transform.DOLocalMove(target, tweenDuration);

            if (useAnimationCurve && tweenCurve != null && tweenCurve.keys.Length > 0)
                tween.SetEase(tweenCurve);
            else
                tween.SetEase(tweenEase);

            _activeTween = tween;
        }
    }
}
