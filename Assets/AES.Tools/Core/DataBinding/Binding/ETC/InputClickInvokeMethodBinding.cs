// InputClickInvokeMethodBinding.cs
using System.Reflection;
using UnityEngine;


namespace AES.Tools
{
    /// <summary>
    /// 클릭 시 ViewModel(또는 path 대상으로) 메서드 호출.
    /// </summary>
    public sealed class InputClickInvokeMethodBinding : ContextBindingBase
    {
        [SerializeField] private string targetPath = "";   // 비우면 root
        [SerializeField] private string methodName = "OnClick";

        private IBindingContext _ctx;
        private object _target;
        private MethodInfo _method;

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _ctx = context;
            ResolveTarget();
        }

        protected override void OnContextUnavailable()
        {
            _ctx    = null;
            _target = null;
            _method = null;
        }

        private void ResolveTarget()
        {
            if (_ctx == null) return;

            _target = _ctx.GetValue(string.IsNullOrEmpty(targetPath) ? null : targetPath);
            if (_target == null || string.IsNullOrEmpty(methodName))
                return;

            _method = _target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, System.Type.EmptyTypes, null);
        }

        private void OnMouseUpAsButton()
        {
            if (_target == null || _method == null)
                ResolveTarget();

            if (_target != null && _method != null)
                _method.Invoke(_target, null);
        }
    }
}