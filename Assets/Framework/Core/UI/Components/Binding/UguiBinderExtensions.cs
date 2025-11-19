using System;
using UnityEngine;
using UnityEngine.UI;
using UnityUtils.Bindable;


// ----------------- 확장/도우미 -----------------

namespace Core.Systems.UI.Components.Binding
{
    /// <summary>
    /// UguiBinderExtensions 클래스는 Unity UI 컴포넌트를 구독 및 처리하기 위한 확장을 제공합니다.
    /// 이 클래스는 Bindable 및 UnityEngine.UI 컴포넌트와의 상호작용을 간결하고 효율적으로 처리할 수 있도록 다양한 확장 메서드를 포함하고 있습니다.
    /// </summary>
    public static class UguiBinderExtensions
    {
        /// <summary>
        /// Bindable를 구독하고, (옵션으로) 현재값을 즉시 발행합니다.
        /// ValueChanged 이벤트를 이용하여 값 변경 시 동작을 정의합니다.
        /// </summary>
        /// <typeparam name="T">Bindable의 값의 타입입니다.</typeparam>
        /// <param name="prop">구독할 Bindable입니다.</param>
        /// <param name="onValue">값 변경 시 호출될 콜백 액션입니다.</param>
        /// <param name="refreshNow">true일 경우 구독 시점에 현재값을 즉시 발행합니다.</param>
        /// <returns>구독을 해제할 IDisposable 객체를 반환합니다.</returns>
        public static IDisposable SubscribeAndReplay<T>(this Bindable<T> prop, Action<T> onValue, bool refreshNow)
        {
            // (old,new) → new 만 전달
            void Handler(T _, T n) => onValue(n);
            prop.ValueChanged += Handler;

            if (refreshNow) prop.Refresh(true);

            return new ActionDisposable(() => prop.ValueChanged -= Handler);
        }

        /// <summary>
        /// InputField.onEndEdit 이벤트를 IDisposable로 래핑합니다.
        /// </summary>
        /// <param name="input">onEndEdit 이벤트를 갖는 InputField 인스턴스입니다.</param>
        /// <param name="handler">onEndEdit 이벤트가 호출될 때 실행될 메서드입니다. 문자열 값을 매개변수로 받습니다.</param>
        /// <returns>onEndEdit 리스너에서 제거 가능한 IDisposable 객체를 반환합니다.</returns>
        public static IDisposable OnEndEditAsDisposable(this InputField input, Action<string> handler)
        {
            void H(string s) => handler(s);
            input.onEndEdit.AddListener(H);
            return new ActionDisposable(() => input.onEndEdit.RemoveListener(H));
        }

        /// <summary>
        /// InputField의 onValueChanged 이벤트를 IDisposable 형태로 래핑합니다.
        /// 이벤트 구독 시 제공된 핸들러를 호출하며, 반환된 IDisposable을 사용하여 구독을 해제할 수 있습니다.
        /// </summary>
        /// <param name="input">onValueChanged 이벤트를 구독할 InputField 객체</param>
        /// <param name="handler">값 변경 시 호출될 콜백 함수</param>
        /// <returns>구독 해제용 IDisposable 객체</returns>
        public static IDisposable OnValueChangedAsDisposable(this InputField input, Action<string> handler)
        {
            void H(string s) => handler(s);
            input.onValueChanged.AddListener(H);
            return new ActionDisposable(() => input.onValueChanged.RemoveListener(H));
        }

        /// <summary>
        /// Slider의 onValueChanged를 IDisposable로 래핑합니다.
        /// </summary>
        /// <param name="slider">onValueChanged 이벤트를 등록할 Slider 객체입니다.</param>
        /// <param name="handler">onValueChanged 이벤트가 발생했을 때 실행될 콜백 함수입니다. Slider의 현재 값을 매개변수로 전달합니다.</param>
        /// <returns>onValueChanged 이벤트에서 등록된 핸들러를 제거하기 위한 IDisposable 객체를 반환합니다.</returns>
        public static IDisposable OnValueChangedAsDisposable(this Slider slider, Action<float> handler)
        {
            void H(float v) => handler(v);
            slider.onValueChanged.AddListener(H);
            return new ActionDisposable(() => slider.onValueChanged.RemoveListener(H));
        }

        /// <summary>
        /// Toggle.onValueChanged 이벤트를 IDisposable로 래핑합니다.
        /// 이벤트 핸들러 등록과 해제를 간편하게 처리할 수 있습니다.
        /// </summary>
        /// <param name="toggle">이벤트를 처리할 Toggle UI 객체입니다.</param>
        /// <param name="handler">값 변경 시 호출될 액션입니다.</param>
        /// <returns>등록된 이벤트 핸들러를 관리하고 해제할 수 있는 IDisposable 객체를 반환합니다.</returns>
        public static IDisposable OnValueChangedAsDisposable(this Toggle toggle, Action<bool> handler)
        {
            void H(bool v) => handler(v);
            toggle.onValueChanged.AddListener(H);
            return new ActionDisposable(() => toggle.onValueChanged.RemoveListener(H));
        }
    

        /// <summary>
        /// Bindable(float) → Image.fillAmount (단방향)
        /// </summary>
        public static IDisposable BindFillAmount(this UguiBinder binder, Image image, Bindable<float> prop, bool refreshNow = true)
        {
            // 모델 → UI
            var d = prop.SubscribeAndReplay(Apply, refreshNow);
            binder.Add(d);
            return d;

            void Apply(float v)
            {
                // 0~1 범위로 안전하게 클램프
                image.fillAmount = Mathf.Clamp01(v);
            }
        }

        /// <summary>
        /// Image.fillAmount ↔ Bindable(float) (양방향)
        /// </summary>
        public static IDisposable BindFillAmountTwoWay(this UguiBinder binder, Image image, Bindable<float> prop, bool refreshNow = true)
        {
            // 모델 → UI
            var d1 = prop.SubscribeAndReplay(v => image.fillAmount = Mathf.Clamp01(v), refreshNow);
            binder.Add(d1);

            // UI → 모델 (필요시 이벤트 트리거 사용)
            var d2 = image.OnValueChangedAsDisposable(v => prop.Value = Mathf.Clamp01(v));
            binder.Add(d2);

            return new CompositeDisposable(d1, d2);
        }

        // --- helper for OnValueChanged (Image는 기본 이벤트가 없으므로, 커스텀 필요 시 확장용 자리) ---
        private static IDisposable OnValueChangedAsDisposable(this Image image, Action<float> handler)
        {
            // Image에는 기본 이벤트가 없으므로 직접 수동 업데이트 시 호출해야 함.
            return new ActionDisposable(() => { }); // no-op disposable
        }

        private sealed class CompositeDisposable : IDisposable
        {
            private IDisposable _a, _b;

            public CompositeDisposable(IDisposable a, IDisposable b)
            {
                _a = a;
                _b = b;
            }

            public void Dispose()
            {
                try { _a?.Dispose(); }
                catch { }

                try { _b?.Dispose(); }
                catch { }

                _a = null;
                _b = null;
            }
        }

        /// <summary>
        /// <c>ActionDisposable</c> 클래스는 주어진 액션(delegate)을 디스포즈 시점에 실행하기 위한 IDisposable 인터페이스의 구현체입니다.
        /// 이 클래스는 주로 이벤트 핸들러를 해제하기 위한 목적으로 사용되며, 액션이 실행된 후 내부적으로 참조를 정리하여 추가 호출을 방지합니다.
        /// </summary>
        private sealed class ActionDisposable : IDisposable
        {
            /// <summary>
            /// Action을 통해 리소스를 정리하는 작업을 캡슐화하는 변수입니다.
            /// 이 변수는 <see cref="IDisposable"/>의 Dispose 메서드 호출 시 제공된 Action을 호출합니다.
            /// </summary>
            private Action _dispose;

            /// <summary>
            /// 주어진 작업(Action)을 Dispose 호출 시 실행할 수 있도록 보장하는 IDisposable 구현 클래스입니다.
            /// 이 클래스를 사용하면 리소스 해제 시 특정 작업을 수행할 수 있습니다.
            /// </summary>
            public ActionDisposable(Action dispose) => _dispose = dispose;

            /// <summary>
            /// IDisposable 인터페이스 구현 메서드로, 리소스 정리 및 해제를 처리합니다.
            /// ActionDisposable 클래스 생성 시 전달된 작업을 실행하며, 호출 이후에는 작업이 한 번만 실행되도록 설정됩니다.
            /// </summary>
            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
