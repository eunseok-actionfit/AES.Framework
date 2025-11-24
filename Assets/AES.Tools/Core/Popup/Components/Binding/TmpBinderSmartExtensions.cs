using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Localization.SmartFormat;


namespace AES.Tools
{
    /// <summary>
    /// <see cref="TMP_Text"/>와 <see cref="Bindable{T}"/>를 연결해 SmartFormat 기반 문자열 바인딩을 제공합니다.
    /// </summary>
    /// <remarks>
    /// - Unity Localization 패키지에 내장된 SmartFormat을 사용합니다.
    /// - 문자열 템플릿 내에서 {key} 형식의 placeholder를 지원합니다.
    /// - 예: <c>"Stage {stage:D2} / Score {score:N0}"</c>
    /// </remarks>
    public static class TmpBinderSmartExtensions
    {
        /// <summary>
        /// SmartFormat 템플릿과 단일 Bindable을 연결합니다.
        /// </summary>
        /// <typeparam name="T1">바인딩할 데이터 타입</typeparam>
        /// <param name="binder">UguiBinder 인스턴스</param>
        /// <param name="tmpText">대상 TMP_Text 컴포넌트</param>
        /// <param name="template">SmartFormat 템플릿 문자열 (예: "Stage {stage}")</param>
        /// <param name="key1">템플릿에서 사용될 placeholder 이름</param>
        /// <param name="prop1">바인딩할 Bindable 인스턴스</param>
        /// <param name="refreshNow">true일 경우 즉시 초기값을 적용</param>
        public static IDisposable BindSmartText<T1>(
            this UguiBinder binder,
            TMP_Text tmpText,
            string template,
            string key1, Bindable<T1> prop1,
            bool refreshNow = true)
        {
            var model = new Dictionary<string, object>();
            void Render()
            {
                model[key1] = prop1 != null ? prop1.Value : null;
                tmpText.text = Smart.Format(template, model);
            }

            var d1 = prop1.SubscribeAndReplay(_ => Render(), refreshNow);
            binder.Add(d1);
            return d1;
        }

        /// <summary>
        /// SmartFormat 템플릿과 두 개의 Bindable을 연결합니다.
        /// </summary>
        /// <typeparam name="T1">첫 번째 데이터 타입</typeparam>
        /// <typeparam name="T2">두 번째 데이터 타입</typeparam>
        /// <param name="binder">UguiBinder 인스턴스</param>
        /// <param name="tmpText">대상 TMP_Text</param>
        /// <param name="template">SmartFormat 템플릿 문자열</param>
        /// <param name="key1">첫 번째 placeholder 이름</param>
        /// <param name="prop1">첫 번째 Bindable</param>
        /// <param name="key2">두 번째 placeholder 이름</param>
        /// <param name="prop2">두 번째 Bindable</param>
        /// <param name="refreshNow">true면 즉시 초기값 적용</param>
        public static IDisposable BindSmartText<T1, T2>(
            this UguiBinder binder,
            TMP_Text tmpText,
            string template,
            string key1, Bindable<T1> prop1,
            string key2, Bindable<T2> prop2,
            bool refreshNow = true)
        {
            var model = new Dictionary<string, object>();
            void Render()
            {
                model[key1] = prop1 != null ? prop1.Value : null;
                model[key2] = prop2 != null ? prop2.Value : null;
                tmpText.text = Smart.Format(template, model);
            }

            var d1 = prop1.SubscribeAndReplay(_ => Render(), refreshNow);
            var d2 = prop2.SubscribeAndReplay(_ => Render(), refreshNow);
            var cd = new Composite2(d1, d2);
            binder.Add(cd);
            return cd;
        }

        /// <summary>
        /// SmartFormat 템플릿과 세 개의 Bindable을 연결합니다.
        /// </summary>
        /// <typeparam name="T1">첫 번째 데이터 타입</typeparam>
        /// <typeparam name="T2">두 번째 데이터 타입</typeparam>
        /// <typeparam name="T3">세 번째 데이터 타입</typeparam>
        /// <param name="binder">UguiBinder 인스턴스</param>
        /// <param name="tmpText">대상 TMP_Text</param>
        /// <param name="template">SmartFormat 템플릿 문자열</param>
        /// <param name="key1">첫 번째 placeholder 이름</param>
        /// <param name="prop1">첫 번째 Bindable</param>
        /// <param name="key2">두 번째 placeholder 이름</param>
        /// <param name="prop2">두 번째 Bindable</param>
        /// <param name="key3">세 번째 placeholder 이름</param>
        /// <param name="prop3">세 번째 Bindable</param>
        /// <param name="refreshNow">true면 즉시 초기값 적용</param>
        public static IDisposable BindSmartText<T1, T2, T3>(
            this UguiBinder binder,
            TMP_Text tmpText,
            string template,
            string key1, Bindable<T1> prop1,
            string key2, Bindable<T2> prop2,
            string key3, Bindable<T3> prop3,
            bool refreshNow = true)
        {
            var model = new Dictionary<string, object>();
            void Render()
            {
                model[key1] = prop1 != null ? prop1.Value : null;
                model[key2] = prop2 != null ? prop2.Value : null;
                model[key3] = prop3 != null ? prop3.Value : null;
                tmpText.text = Smart.Format(template, model);
            }

            var d1 = prop1.SubscribeAndReplay(_ => Render(), refreshNow);
            var d2 = prop2.SubscribeAndReplay(_ => Render(), refreshNow);
            var d3 = prop3.SubscribeAndReplay(_ => Render(), refreshNow);
            var cd = new Composite3(d1, d2, d3);
            binder.Add(cd);
            return cd;
        }

        // ────────────────────────────────────────────────────────────────────────────────
        /// <summary>두 개의 IDisposable을 묶어서 함께 해제하는 도우미 클래스입니다.</summary>
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

        /// <summary>세 개의 IDisposable을 묶어서 함께 해제하는 도우미 클래스입니다.</summary>
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
    }
}