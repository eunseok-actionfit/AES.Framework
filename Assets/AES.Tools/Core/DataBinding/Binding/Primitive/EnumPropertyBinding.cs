// EnumPropertyBinding.cs
using System;
using UnityEngine;
using UnityEngine.Events;
using AES.Tools;

public sealed class EnumPropertyBinding : ContextBindingBase
{
    [SerializeField] private UnityEvent<int> onChanged;

    private Action<object> _listener;
    private object _token;

    protected override void OnContextAvailable(IBindingContext ctx, string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        _listener = v =>
        {
            if (v is Enum e)
            {
                int intValue = Convert.ToInt32(e);
                onChanged?.Invoke(intValue);
#if UNITY_EDITOR
                Debug_OnValueUpdated(e, ResolvedPath);
#endif
            }
            // 필요하면 여기서 string 이름으로도 이벤트 한 번 더 쏘는 구조 추가 가능
        };

        _token = ctx.RegisterListener(path, _listener);
    }

    protected override void OnContextUnavailable()
    {
        if (BindingContext != null && _listener != null)
            BindingContext.RemoveListener(ResolvedPath, _listener, _token);
    }
}