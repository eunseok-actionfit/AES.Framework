using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using AES.Tools;


public static class BindingContextExtensions
{
    private static readonly ConditionalWeakTable<LambdaExpression, string> PathCache
        = new();

    private static string GetOrAddPath(LambdaExpression lambda)
    {
        if (!PathCache.TryGetValue(lambda, out var path))
        {
            path = ExtractPath(lambda.Body);
            PathCache.Add(lambda, path);
        }
        return path;
    }

    public static IDisposable SubscribeProperty<TViewModel, TValue>(
        this IBindingContext ctx,
        Expression<Func<TViewModel, IObservableProperty<TValue>>> selector,
        Action<TValue> onChanged)
    {
        var lambda = selector;
        string path = GetOrAddPath(lambda);

        Action<object> boxed = v =>
        {
            if (v is TValue tv)
                onChanged(tv);
        };

        var token = ctx.RegisterListener(path, boxed);

        return new BindingSubscription(ctx, path, boxed, token);
    }

    public static IDisposable SubscribeProperty<TViewModel, TValue>(
        this IBindingContext ctx,
        AES.Tools.BindingBehaviour owner,   // ← 추가
        Expression<Func<TViewModel, IObservableProperty<TValue>>> selector,
        Action<TValue> onChanged)
    {
        var lambda = selector;
        string path = GetOrAddPath(lambda);

        Action<object> boxed = v =>
        {
#if UNITY_EDITOR
            owner?.Debug_OnValueUpdated(v, path);
#endif
            if (v is TValue tv)
                onChanged(tv);
        };

        var token = ctx.RegisterListener(path, boxed);

        return new BindingSubscription(ctx, path, boxed, token);
    }


    private static string ExtractPath(Expression expr)
    {
        if (expr is UnaryExpression u && u.NodeType == ExpressionType.Convert)
            expr = u.Operand;

        var parts = new Stack<string>();
        var cur = expr;

        while (cur is MemberExpression m)
        {
            parts.Push(m.Member.Name);
            cur = m.Expression;
        }

        return string.Join(".", parts);
    }

    private sealed class BindingSubscription : IDisposable
    {
        private readonly IBindingContext _ctx;
        private readonly string          _path;
        private readonly Action<object>  _boxed;
        private readonly object          _token;
        private bool                     _disposed;

        public BindingSubscription(
            IBindingContext ctx,
            string path,
            Action<object> boxed,
            object token)
        {
            _ctx   = ctx;
            _path  = path;
            _boxed = boxed;
            _token = token;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 1순위: IDisposable 토큰이면 그냥 Dispose()
            if (_token is IDisposable d)
            {
                d.Dispose();
            }
            else
            {
                // 구버전 컨텍스트 호환용
                _ctx.RemoveListener(_path, _boxed, _token);
            }
        }
    }

}
