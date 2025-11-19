using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Localization;
using UnityUtils.Bindable;


namespace Core.Systems.UI.Components.Binding
{
    public static class TmpBinderLocalizedSmartExtensions
    {
        // 1개
        public static IDisposable BindLocalizedSmartText<T1>(
            this UguiBinder binder,
            TMP_Text tmpText,
            LocalizedString localized, // 테이블/엔트리 참조
            string key1, Bindable<T1> prop1,
            bool refreshNow = true)
        {
            var model = new Dictionary<string, object>();
            void RenderAndRefresh()
            {
                model[key1] = prop1 != null ? prop1.Value : null;
                localized.Arguments = new object[] { model }; // 딕셔너리 1개로 named placeholder 지원
                localized.RefreshString(); // 인자 변경 반영
            }

            // 로케일/문자열이 바뀌면 TMP에 반영
            void OnChanged(string s) => tmpText.text = s;
            localized.StringChanged += OnChanged;

            var d1 = prop1.SubscribeAndReplay(_ => RenderAndRefresh(), refreshNow);
            // 초기 렌더 (refreshNow=false 인 상황 대비)
            if (!refreshNow) RenderAndRefresh();

            // 정리용 디스포저
            return new Composite2(
                d1,
                new ActionDisposable(() => localized.StringChanged -= OnChanged)
            );
        }

        // 2개
        public static IDisposable BindLocalizedSmartText<T1, T2>(
            this UguiBinder binder,
            TMP_Text tmpText,
            LocalizedString localized,
            string key1, Bindable<T1> prop1,
            string key2, Bindable<T2> prop2,
            bool refreshNow = true)
        {
            var model = new Dictionary<string, object>();
            void RenderAndRefresh()
            {
                model[key1] = prop1 != null ? prop1.Value : null;
                model[key2] = prop2 != null ? prop2.Value : null;
                localized.Arguments = new object[] { model };
                localized.RefreshString();
            }

            void OnChanged(string s) => tmpText.text = s;
            localized.StringChanged += OnChanged;

            var d1 = prop1.SubscribeAndReplay(_ => RenderAndRefresh(), refreshNow);
            var d2 = prop2.SubscribeAndReplay(_ => RenderAndRefresh(), refreshNow);
            if (!refreshNow) RenderAndRefresh();

            var cd = new Composite3(
                d1, d2,
                new ActionDisposable(() => localized.StringChanged -= OnChanged)
            );
            binder.Add(cd);
            return cd;
        }

        // 3개
        public static IDisposable BindLocalizedSmartText<T1, T2, T3>(
            this UguiBinder binder,
            TMP_Text tmpText,
            LocalizedString localized,
            string key1, Bindable<T1> prop1,
            string key2, Bindable<T2> prop2,
            string key3, Bindable<T3> prop3,
            bool refreshNow = true)
        {
            var model = new Dictionary<string, object>();
            void RenderAndRefresh()
            {
                model[key1] = prop1 != null ? prop1.Value : null;
                model[key2] = prop2 != null ? prop2.Value : null;
                model[key3] = prop3 != null ? prop3.Value : null;
                localized.Arguments = new object[] { model };
                localized.RefreshString();
            }

            void OnChanged(string s) => tmpText.text = s;
            localized.StringChanged += OnChanged;

            var d1 = prop1.SubscribeAndReplay(_ => RenderAndRefresh(), refreshNow);
            var d2 = prop2.SubscribeAndReplay(_ => RenderAndRefresh(), refreshNow);
            var d3 = prop3.SubscribeAndReplay(_ => RenderAndRefresh(), refreshNow);
            if (!refreshNow) RenderAndRefresh();

            var cd = new Composite4(
                d1, d2, d3,
                new ActionDisposable(() => localized.StringChanged -= OnChanged)
            );
            binder.Add(cd);
            return cd;
        }

        // ── helpers ──────────────────────────────────────────────────────────────
        private sealed class ActionDisposable : IDisposable
        {
            private Action _dispose;
            public ActionDisposable(Action dispose) => _dispose = dispose;

            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }

        private sealed class Composite2 : IDisposable
        {
            private IDisposable _a, _b;

            public Composite2(IDisposable a, IDisposable b)
            {
                _a = a;
                _b = b;
            }

            public void Dispose()
            {
                try { _a?.Dispose(); }
                catch
                { // ignored
                }

                try { _b?.Dispose(); }
                catch
                { // ignored
                }

                _a = _b = null;
            }
        }

        private sealed class Composite3 : IDisposable
        {
            private IDisposable _a, _b, _c;

            public Composite3(IDisposable a, IDisposable b, IDisposable c)
            {
                _a = a;
                _b = b;
                _c = c;
            }

            public void Dispose()
            {
                try { _a?.Dispose(); }
                catch
                { // ignored
                }

                try { _b?.Dispose(); }
                catch
                { // ignored
                }

                try { _c?.Dispose(); }
                catch
                { // ignored
                }

                _a = _b = _c = null;
            }
        }

        private sealed class Composite4 : IDisposable
        {
            private IDisposable _a, _b, _c, _d;

            public Composite4(IDisposable a, IDisposable b, IDisposable c, IDisposable d)
            {
                _a = a;
                _b = b;
                _c = c;
                _d = d;
            }

            public void Dispose()
            {
                try { _a?.Dispose(); }
                catch
                { // ignored
                }

                try { _b?.Dispose(); }
                catch
                { // ignored
                }

                try { _c?.Dispose(); }
                catch
                { // ignored
                }

                try { _d?.Dispose(); }
                catch
                { // ignored
                }

                _a = _b = _c = _d = null;
            }
        }
    }
}