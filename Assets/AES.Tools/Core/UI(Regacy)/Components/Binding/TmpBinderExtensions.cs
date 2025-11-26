using System;
using TMPro;
using UnityEngine;


// TMP_Text 바인딩 (단방향)
namespace AES.Tools
{
    public static class TmpBinderExtensions
    {
        /// <summary>Bindable → TMP_Text.text (단방향)</summary>
        public static IDisposable BindText(this UguiBinder binder, TMP_Text tmpText, Bindable<string> prop, bool refreshNow = true)
        {
            var d = prop.SubscribeAndReplay(Apply, refreshNow);
            binder.Add(d);
            return d;
            void Apply(string v)
            {
                tmpText.text = v ?? string.Empty;
                Debug.Log(v);
            }
        }

        /// <summary>Bindable(int/float 등) → TMP_Text.text (단방향, format 지원)</summary>
        public static IDisposable BindText<T>(this UguiBinder binder, TMP_Text tmpText, Bindable<T> prop, string format = "{0}", bool refreshNow = true)
        {
            var d = prop.SubscribeAndReplay(Apply, refreshNow);
            binder.Add(d);
            return d;
            void Apply(T v) => tmpText.text = string.Format(format, v);
        }

        /// <summary>TMP_InputField.text ↔ Bindable(string) (양방향)</summary>
        // public static IDisposable BindInputFieldText(this UguiBinder binder, TMP_InputField input, Bindable<string> prop, bool refreshNow = true, bool commitOnEndEdit = true)
        // {
        //     // 모델→UI
        //     var d1 = prop.SubscribeAndReplay(v => input.SetTextWithoutNotify(v ?? string.Empty), refreshNow);
        //     binder.Add(d1);
        //
        //     // UI→모델
        //     void Commit(string s) => prop.Value = s ?? string.Empty;
        //     if (commitOnEndEdit)
        //     {
        //         var d2 = input.OnEndEditAsDisposable(Commit);
        //         binder.Add(d2);
        //         return new CompositeDisposable(d1, d2);
        //     }
        //     else
        //     {
        //         var d2 = input.OnValueChangedAsDisposable(Commit);
        //         binder.Add(d2);
        //         return new CompositeDisposable(d1, d2);
        //     }
        // }

        // TMP Input 확장 메서드
        public static IDisposable OnEndEditAsDisposable(this TMP_InputField input, Action<string> handler)
        {
            input.onEndEdit.AddListener(H);
            return new ActionDisposable(() => input.onEndEdit.RemoveListener(H));
            void H(string s) => handler(s);
        }

        public static IDisposable OnValueChangedAsDisposable(this TMP_InputField input, Action<string> handler)
        {
            void H(string s) => handler(s);
            input.onValueChanged.AddListener(H);
            return new ActionDisposable(() => input.onValueChanged.RemoveListener(H));
        }

        // 공용 Disposable들
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
    }
}