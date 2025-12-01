// InputToggleBoolBinding.cs
namespace AES.Tools
{
    /// <summary>
    /// 클릭 시 bool 프로퍼티를 토글.
    /// path 가 "Selected" 같은 bool 이라고 가정.
    /// </summary>
    public sealed class InputToggleBoolBinding : ContextBindingBase
    {
        private IBindingContext _ctx;

        protected override void OnContextAvailable(IBindingContext context, string path)
        {
            _ctx = context;
        }

        protected override void OnContextUnavailable()
        {
            _ctx = null;
        }

        private void OnMouseUpAsButton()
        {
            if (_ctx == null || string.IsNullOrEmpty(ResolvedPath))
                return;

            var current = _ctx.GetValue(ResolvedPath);
            if (current is bool b)
                _ctx.SetValue(ResolvedPath, !b);
        }
    }
}