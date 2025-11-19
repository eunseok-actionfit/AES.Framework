using System;
using System.Globalization;
using TMPro;
using UnityEngine.UI;
using UnityUtils.Bindable;


// 프로젝트에 이미 있는 것으로 가정:
// - UguiBinder
// - Bindable<T>
// - OnEndEditAsDisposable / OnValueChangedAsDisposable
// - CompositeDisposable
namespace Core.Systems.UI.Components.Binding
{
    public static class InputBinderExtensions
    {
        /// <summary>
        /// uGUI InputField ⟷ Bindable&lt;T&gt; (양방향).
        /// 기본 포맷터/파서는 숫자/불리언/열거형을 지원하며, 커스텀도 주입 가능.
        /// </summary>
        public static IDisposable BindInputField<T>(
            this UguiBinder binder,
            InputField input,
            Bindable<T> prop,
            Func<T, string> toText = null,
            Func<string, (bool ok, T value)> tryParse = null,
            bool refreshNow = true,
            bool commitOnEndEdit = true,
            T fallbackOnParseFail = default)
        {
            toText ??= DefaultToText;
            tryParse ??= DefaultTryParse<T>;

            // 모델 → UI
            var d1 = prop.SubscribeAndReplay(v => input.SetTextWithoutNotify(SafeToText(toText, v)), refreshNow);
            binder.Add(d1);

            // UI → 모델
            void Commit(string s)
            {
                var (ok, v) = SafeTryParse(tryParse, s);
                prop.Value = ok ? v : fallbackOnParseFail;
            }

            var d2 = commitOnEndEdit
                ? input.OnEndEditAsDisposable(Commit)
                : input.OnValueChangedAsDisposable(Commit);

            binder.Add(d2);
            return new UguiBinder.CompositeDisposable(d1, d2);
        }

        /// <summary>
        /// TMP_InputField ⟷ Bindable&lt;T&gt; (양방향).
        /// </summary>
        public static IDisposable BindTmpInputField<T>(
            this UguiBinder binder,
            TMP_InputField input,
            Bindable<T> prop,
            Func<T, string> toText = null,
            Func<string, (bool ok, T value)> tryParse = null,
            bool refreshNow = true,
            bool commitOnEndEdit = true,
            T fallbackOnParseFail = default)
        {
            toText ??= DefaultToText;
            tryParse ??= DefaultTryParse<T>;

            // 모델 → UI
            var d1 = prop.SubscribeAndReplay(v => input.SetTextWithoutNotify(SafeToText(toText, v)), refreshNow);
            binder.Add(d1);

            // UI → 모델
            void Commit(string s)
            {
                var (ok, v) = SafeTryParse(tryParse, s);
                prop.Value = ok ? v : fallbackOnParseFail;
            }

            var d2 = commitOnEndEdit
                ? input.OnEndEditAsDisposable(Commit)
                : input.OnValueChangedAsDisposable(Commit);

            binder.Add(d2);
            return new UguiBinder.CompositeDisposable(d1, d2);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // 기본 포맷 & 파서
        private static string SafeToText<T>(Func<T, string> toText, T v)
        {
            try { return toText?.Invoke(v) ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static (bool ok, T value) SafeTryParse<T>(Func<string, (bool ok, T value)> tryParse, string s)
        {
            try { return tryParse?.Invoke(s) ?? (false, default); }
            catch { return (false, default); }
        }

        private static string DefaultToText<T>(T v)
        {
            if (v is null) return string.Empty;
            if (v is IFormattable f) return f.ToString(null, CultureInfo.InvariantCulture);
            return v.ToString();
        }

        private static (bool ok, T value) DefaultTryParse<T>(string s)
        {
            s ??= string.Empty;
            var t = typeof(T);

            object boxed;

            if (t == typeof(string)) { boxed = s; return (true, (T)boxed); }

            // bool
            if (t == typeof(bool)) {
                if (bool.TryParse(s, out var b)) { boxed = b; return (true, (T)boxed); }
                // 0/1도 허용
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bi)) {
                    boxed = bi != 0; return (true, (T)boxed);
                }
                return (false, default);
            }

            // 정수류
            if (t == typeof(int)) {
                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) { boxed = v; return (true, (T)boxed); }
                return (false, default);
            }
            if (t == typeof(long)) {
                if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) { boxed = v; return (true, (T)boxed); }
                return (false, default);
            }
            if (t == typeof(short)) {
                if (short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) { boxed = v; return (true, (T)boxed); }
                return (false, default);
            }
            if (t == typeof(byte)) {
                if (byte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) { boxed = v; return (true, (T)boxed); }
                return (false, default);
            }

            // 부동소수/십진
            if (t == typeof(float)) {
                if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v)) { boxed = v; return (true, (T)boxed); }
                return (false, default);
            }
            if (t == typeof(double)) {
                if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var v)) { boxed = v; return (true, (T)boxed); }
                return (false, default);
            }
            if (t == typeof(decimal)) {
                if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) { boxed = v; return (true, (T)boxed); }
                return (false, default);
            }

            // 열거형(대소문자 무시, 공백 트림)
            if (t.IsEnum) {
                if (Enum.TryParse(t, s.Trim(), ignoreCase: true, out var ev))
                    return (true, (T)ev);
                return (false, default);
            }

            // 그 외 타입은 실패(커스텀 tryParse를 주입해서 사용)
            return (false, default);
        }
    }
}
