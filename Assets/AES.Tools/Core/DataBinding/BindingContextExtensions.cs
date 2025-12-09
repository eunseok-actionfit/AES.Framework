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
        BindingBehaviour owner,   // ← 추가
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

     public static IDisposable SubscribeList<TViewModel, TItem>(
        this IBindingContext ctx,
        Expression<Func<TViewModel, ObservableList<TItem>>> selector,
        Action<ObservableList<TItem>> onReset,
        Action<int, TItem> onItemAdded,
        Action<int, TItem> onItemRemoved)
    {
        var lambda = selector;
        string path = GetOrAddPath(lambda);

        ObservableList<TItem> current = null;

        // 이벤트 핸들러 정의
        void HandleReset()
        {
            if (current != null)
                onReset?.Invoke(current);
        }

        void HandleItemAdded(int index, TItem item)
        {
            onItemAdded?.Invoke(index, item);
        }

        void HandleItemRemoved(int index, TItem item)
        {
            onItemRemoved?.Invoke(index, item);
        }

        // 리스트 교체 + 핸들러 재연결
        void AttachToList(ObservableList<TItem> list)
        {
            // 이전 리스트 Detach
            if (current != null)
            {
                current.OnListChanged -= HandleReset;
                current.ItemAdded     -= HandleItemAdded;
                current.ItemRemoved   -= HandleItemRemoved;
            }

            current = list;

            if (current == null)
                return;

            // 필요할 때만 구독
            if (onReset != null)
                current.OnListChanged += HandleReset;

            if (onItemAdded != null)
                current.ItemAdded += HandleItemAdded;

            if (onItemRemoved != null)
                current.ItemRemoved += HandleItemRemoved;

            // 초기 상태 한 번 알려줌
            onReset?.Invoke(current);
        }

        // BindingContext에서 해당 경로 값이 바뀔 때마다 호출
        Action<object> boxed = v =>
        {
            // null 이거나 타입이 다르면 Detach만 하고 끝
            if (v is ObservableList<TItem> list)
            {
                AttachToList(list);
            }
            else
            {
                AttachToList(null);
            }
        };

        var token = ctx.RegisterListener(path, boxed);

        // Dispose 시: BindingContext 리스너 해제 + 리스트 이벤트 해제
        return new BindingSubscription(
            ctx,
            path,
            boxed,
            token,
            onDispose: () =>
            {
                if (current != null)
                {
                    current.OnListChanged -= HandleReset;
                    current.ItemAdded     -= HandleItemAdded;
                    current.ItemRemoved   -= HandleItemRemoved;
                    current = null;
                }
            });
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
        private readonly Action          _onDispose;
        private bool                     _disposed;

        public BindingSubscription(
            IBindingContext ctx,
            string path,
            Action<object> boxed,
            object token,
            Action onDispose = null)
        {
            _ctx       = ctx;
            _path      = path;
            _boxed     = boxed;
            _token     = token;
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 리스트 등 부가 자원 정리
            _onDispose?.Invoke();

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


