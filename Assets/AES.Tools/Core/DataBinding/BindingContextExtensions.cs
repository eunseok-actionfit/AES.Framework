using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AES.Tools
{
    public static class BindingContextExtensions
    {
        // =========================
        // 기존: object 기반 버전
        // =========================

        public static object RegisterProperty<TViewModel, TValue>(
            this IBindingContext ctx,
            Expression<Func<TViewModel, IObservableProperty<TValue>>> selector,
            Action<object> onChanged)
        {
            string path = ExtractPath(selector.Body);
            return ctx.RegisterListener(path, onChanged);
        }

        public static void RemoveProperty<TViewModel, TValue>(
            this IBindingContext ctx,
            Expression<Func<TViewModel, IObservableProperty<TValue>>> selector,
            Action<object> listener,
            object token)
        {
            string path = ExtractPath(selector.Body);
            ctx.RemoveListener(path, listener, token);
        }


        // =========================
        // 새로 추가: 타입 안전 버전 (Action<TValue>)
        // =========================

        public static object RegisterProperty<TViewModel, TValue>(
            this IBindingContext ctx,
            Expression<Func<TViewModel, IObservableProperty<TValue>>> selector,
            Action<TValue> onChanged)
        {
            void Boxed(object v)
            {
                if (v is TValue tv) onChanged(tv);
            }

            return ctx.RegisterProperty(selector, (Action<object>)Boxed);
        }

        public static void RemoveProperty<TViewModel, TValue>(
            this IBindingContext ctx,
            Expression<Func<TViewModel, IObservableProperty<TValue>>> selector,
            Action<TValue> listener,
            object token)
        {
            // token 기반 제거라 listener는 필요 없음
            ctx.RemoveProperty(selector, (Action<object>)null, token);
        }


        // =========================
        // Path 추출
        // =========================

        private static string ExtractPath(Expression expr)
        {
            var parts = new Stack<string>();
            var cur = expr;

            while (cur is MemberExpression m)
            {
                parts.Push(m.Member.Name);
                cur = m.Expression;
            }

            return string.Join(".", parts);
        }
    }
}
