using System;
using UnityEngine;

namespace AES.Tools
{
    public sealed class TransformLocalPositionBinding : ContextBindingBase
    {
        private Action<object> _listener;
        private object         _token;

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
            // 권장: IDisposable 토큰이면 Dispose
            if (_token is IDisposable d)
            {
                d.Dispose();
            }
            else if (BindingContext != null && _listener != null)
            {
                // 구버전 컨텍스트 대응
                BindingContext.RemoveListener(ResolvedPath, _listener, _token);
            }

            _listener = null;
            _token    = null;
        }

        private void OnValueChanged(object value)
        {
#if UNITY_EDITOR
            Debug_SetLastValue(value);
#endif
            if (value is Vector3 v3)
                transform.localPosition = v3;
            else if (value is Vector2 v2)
                transform.localPosition = new Vector3(v2.x, v2.y, transform.localPosition.z);
        }
    }
}