using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;


// 사용 예:
// var binder = new UguiBinder(this); // MonoBehaviour 전달(자동 해제)
// binder.BindText(hpText, vm.Health);              // 모델→UI
// binder.BindSliderValue(hpSlider, vm.Health, 0, 100); // 양방향
// binder.BindInputFieldText(nameInput, vm.Name);   // 양방향
namespace AES.Tools.Components.Binding
{
    /// <summary>
    /// UguiBinder는 Unity UI와 ViewModel 사이의 데이터 바인딩을 간편하게 구현하기 위한 헬퍼 클래스입니다.
    /// MonoBehaviour 인스턴스에서 동작하며, 제공된 Bind 메서드를 통해 UI 요소와 바인더블 프로퍼티(Bindable)를
    /// 연결할 수 있습니다. 자동 Dispose 지원과 구독 해제 기능을 포함하고 있습니다.
    /// </summary>
    public sealed class UguiBinder : IDisposable
    {
        /// <summary>
        /// IDisposable 객체들을 저장하고 관리하는 컬렉션입니다.
        /// UguiBinder 클래스가 관리하는 리소스들을 이 컬렉션을 통해 추적하며,
        /// Dispose 메서드 호출 시 컬렉션 내의 모든 객체를 해제합니다.
        /// </summary>
        private readonly List<IDisposable> _disposables = new();
        /// <summary>
        /// 객체의 Dispose 상태를 나타내는 내부 플래그입니다.
        /// true일 경우 해당 객체가 이미 Dispose된 상태임을 의미합니다.
        /// 일부 메서드에서 객체가 이미 Dispose된 상태인지 확인하여 추가 작업을 방지하거나
        /// 리소스 관리의 안정성을 확보하는 데 사용됩니다.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// MonoBehaviour를 넘기면 OnDestroy 시 자동으로 Dispose 됩니다.
        /// (Awake/Start에서 생성 권장)
        /// </summary>
        public UguiBinder(MonoBehaviour owner = null)
        {
            if (owner != null)
            {
                // 파괴 시점까지 대기 → Dispose
                AutoDisposeOnDestroyAsync(owner).Forget();
            }
        }

        /// <summary>
        /// MonoBehaviour의 파괴 시점을 감지하여 자동으로 Dispose를 수행합니다.
        /// </summary>
        /// <param name="owner">파괴 시점을 감지할 대상 MonoBehaviour입니다.</param>
        /// <returns>비동기 작업 결과를 반환하지 않습니다.</returns>
        private async UniTaskVoid AutoDisposeOnDestroyAsync(MonoBehaviour owner)
        {
            var token = owner.GetCancellationTokenOnDestroy(); // (= destroyCancellationToken)

            try
            {
                // 오브젝트 파괴될 때까지 대기
                await UniTask.WaitUntilCanceled(token);
            }
            finally { Dispose(); }
        }

        // ----------------- 공통 헬퍼 -----------------

        /// <summary>
        /// IDisposable 객체를 추가하여 관리합니다.
        /// 추가된 객체는 UguiBinder가 Dispose될 때 함께 해제됩니다.
        /// </summary>
        /// <param name="d">관리할 IDisposable 객체</param>
        public void Add(IDisposable d)
        {
            if (_disposed)
            {
                d.Dispose();
                return;
            }

            _disposables.Add(d);
        }

        /// <summary>
        /// UguiBinder가 보유한 모든 IDisposable 객체를 해제하고 자원을 정리합니다.
        /// UguiBinder가 더 이상 사용되지 않을 때 호출되어야 합니다.
        /// 중복 호출을 방지하기 위해 내부적으로 상태를 확인하고, 한번만 Dispose가 실행됩니다.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var d in _disposables)
                try { d?.Dispose(); }
                catch
                { /* swallow */
                }

            _disposables.Clear();
        }

        // ----------------- 바인딩 (모델→UI) -----------------

        /// <summary>
        /// Bindable의 값을 Text 컴포넌트의 text 속성에 바인딩합니다.
        /// 모델에서 UI로의 단방향 바인딩을 설정합니다.
        /// </summary>
        /// <param name="text">값을 표시할 Text 컴포넌트</param>
        /// <param name="prop">바인딩할 Bindable</param>
        /// <param name="refreshNow">초기 값을 즉시 적용할지 여부</param>
        /// <returns>구독이 포함된 IDisposable 객체</returns>
        public IDisposable BindText(Text text, Bindable<string> prop, bool refreshNow = true)
        {
            var d = prop.SubscribeAndReplay(Apply, refreshNow);
            Add(d);
            return d;
            void Apply(string v) => text.text = v ?? string.Empty;
        }

        /// <summary>
        /// Bindable 값을 Text 컴포넌트의 text 속성에 연결합니다.
        /// 값이 변경되면 지정된 형식(format)에 따라 자동으로 반영됩니다.
        /// </summary>
        /// <typeparam name="T">Bindable의 값 타입</typeparam>
        /// <param name="text">값을 반영할 Text 컴포넌트</param>
        /// <param name="prop">값을 제공할 Bindable 인스턴스</param>
        /// <param name="format">값을 출력할 형식 지정 문자열(기본값: "{0}")</param>
        /// <param name="refreshNow">바로 값을 갱신할지 여부</param>
        /// <returns>바인딩을 관리하는 IDisposable 객체</returns>
        public IDisposable BindText<T>(Text text, Bindable<T> prop, string format = "{0}", bool refreshNow = true)
        {
            var d = prop.SubscribeAndReplay(Apply, refreshNow);
            Add(d);
            return d;
            void Apply(T v) => text.text = string.Format(format, v);
        }

        /// <summary>
        /// Bindable(bool)을 GameObject.SetActive와 연결하여 단방향으로 바인딩합니다.
        /// </summary>
        /// <param name="go">활성화 상태를 조작할 GameObject</param>
        /// <param name="prop">GameObject의 SetActive 상태를 바인딩할 Bindable<bool></param>
        /// <param name="refreshNow">초기 바인딩 시 즉시 값을 적용할지 여부를 결정하는 플래그</param>
        /// <returns>해제 가능하도록 IDisposable로 반환</returns>
        public IDisposable BindActive(GameObject go, Bindable<bool> prop, bool refreshNow = true)
        {
            void Apply(bool v) => go.SetActive(v);
            var d = prop.SubscribeAndReplay(Apply, refreshNow);
            Add(d);
            return d;
        }

        /// <summary>
        /// Bindable(float)의 값 변화를 Slider.value에 적용하는 단방향 바인딩을 수행합니다.
        /// Slider의 UI 요소 값이 실시간으로 업데이트됩니다.
        /// </summary>
        /// <param name="slider">값을 바인딩할 UnityEngine.UI.Slider 요소</param>
        /// <param name="prop">바인딩할 Bindable<float> 값</param>
        /// <param name="refreshNow">바인딩 시점에 즉시 값을 업데이트할지 여부</param>
        /// <returns>구독 해제를 위한 IDisposable 객체</returns>
        public IDisposable BindSliderValue(Slider slider, Bindable<float> prop, bool refreshNow = true)
        {
            var d = prop.SubscribeAndReplay(Apply, refreshNow);
            Add(d);
            return d;
            void Apply(float v) => slider.SetValueWithoutNotify(v);
        }

        /// <summary>
        /// Slider.value와 Bindable(int)을 연동하며, Slider의 최소값과 최대값을 지정할 수 있습니다.
        /// 슬라이더는 정수 단위로 동작합니다.
        /// </summary>
        /// <param name="slider">연동할 Slider 컴포넌트</param>
        /// <param name="prop">연동할 Bindable(int) 객체</param>
        /// <param name="min">Slider의 최소값</param>
        /// <param name="max">Slider의 최대값</param>
        /// <param name="refreshNow">초기화 시 즉시 값을 연동할지 여부. 기본값은 true</param>
        /// <returns>바인딩을 관리하는 IDisposable 객체</returns>
        public IDisposable BindSliderValue(Slider slider, Bindable<int> prop, int min, int max, bool refreshNow = true)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
            var d = prop.SubscribeAndReplay(Apply, refreshNow);
            Add(d);
            return d;
            void Apply(int v) => slider.SetValueWithoutNotify(v);
        }

        // ----------------- 바인딩 (양방향) -----------------

        /// <summary>
        /// InputField의 text 값을 Bindable<string>과 양방향으로 바인딩합니다.
        /// </summary>
        /// <param name="input">바인딩할 InputField 입니다.</param>
        /// <param name="prop">바인딩할 Bindable<string> 입니다.</param>
        /// <param name="refreshNow">true로 설정하면 즉시 모델 값을 UI에 반영합니다.</param>
        /// <param name="commitOnEndEdit">true로 설정하면 InputField의 편집이 끝날 때 모델 값에 반영됩니다.
        /// false로 설정하면 텍스트 변경 시마다 모델 값에 반영됩니다.</param>
        /// <returns>바인딩을 관리하기 위한 IDisposable 객체를 반환합니다.</returns>
        public IDisposable BindInputFieldText(InputField input, Bindable<string> prop, bool refreshNow = true, bool commitOnEndEdit = true)
        {
            // 모델→UI
            var d1 = prop.SubscribeAndReplay(v => input.SetTextWithoutNotify(v ?? string.Empty), refreshNow);
            Add(d1);

            // UI→모델
            void Commit(string s) => prop.Value = s ?? string.Empty;

            if (commitOnEndEdit)
            {
                var d2 = input.OnEndEditAsDisposable(Commit);
                Add(d2);
                return new CompositeDisposable(d1, d2);
            }
            else
            {
                var d2 = input.OnValueChangedAsDisposable(Commit);
                Add(d2);
                return new CompositeDisposable(d1, d2);
            }
        }

        /// <summary>
        /// Slider의 값(value)과 Bindable(float)을 양방향으로 바인딩합니다.
        /// </summary>
        /// <param name="slider">연결할 Slider 객체입니다.</param>
        /// <param name="prop">바인딩할 Bindable<float>입니다.</param>
        /// <param name="min">Slider의 최소값입니다.</param>
        /// <param name="max">Slider의 최대값입니다.</param>
        /// <param name="refreshNow">초기 바인딩 시 즉시 값을 갱신할지 여부입니다.</param>
        /// <returns>바인딩 처리를 담당하는 IDisposable 객체를 반환합니다.</returns>
        public IDisposable BindSliderValue(Slider slider, Bindable<float> prop, float min, float max, bool refreshNow = true)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;

            // 모델→UI
            var d1 = prop.SubscribeAndReplay(slider.SetValueWithoutNotify, refreshNow);
            Add(d1);

            // UI→모델
            var d2 = slider.OnValueChangedAsDisposable(v => prop.Value = v);
            Add(d2);

            return new CompositeDisposable(d1, d2);
        }

        /// <summary>
        /// Toggle의 isOn 상태와 Bindable(bool)을 양방향으로 연동합니다.
        /// </summary>
        /// <param name="toggle">isOn 상태를 연동할 대상 Toggle입니다.</param>
        /// <param name="prop">연동할 Bindable입니다.</param>
        /// <param name="refreshNow">true일 경우 현재 상태를 즉시 반영합니다.</param>
        /// <returns>연동을 관리하기 위한 IDisposable 객체를 반환합니다.</returns>
        public IDisposable BindToggle(Toggle toggle, Bindable<bool> prop, bool refreshNow = true)
        {
            var d1 = prop.SubscribeAndReplay(toggle.SetIsOnWithoutNotify, refreshNow);
            Add(d1);

            var d2 = toggle.OnValueChangedAsDisposable(v => prop.Value = v);
            Add(d2);

            return new CompositeDisposable(d1, d2);
        }

        /// <summary>
        /// Bindable(bool)을 Selectable.interactable에 바인딩합니다.
        /// 모델에서 UI로의 단방향 바인딩을 설정합니다.
        /// </summary>
        /// <param name="selectable">interactable 값을 제어할 Selectable (Button, Toggle, Slider 등)</param>
        /// <param name="prop">바인딩할 Bindable<bool></param>
        /// <param name="refreshNow">초기 값을 즉시 적용할지 여부</param>
        /// <returns>구독이 포함된 IDisposable 객체</returns>
        public IDisposable BindInteractable(Selectable selectable, Bindable<bool> prop, bool refreshNow = true)
        {
            void Apply(bool v) => selectable.interactable = v;
            var d = prop.SubscribeAndReplay(Apply, refreshNow);
            Add(d);
            return d;
        }

        /// <summary>
        /// Bindable(bool)을 Button.interactable에 바인딩합니다.
        /// 내부적으로 BindInteractable(Button, ...)을 사용합니다.
        /// </summary>
        /// <param name="button">interactable 값을 제어할 Button</param>
        /// <param name="prop">바인딩할 Bindable<bool></param>
        /// <param name="refreshNow">초기 값을 즉시 적용할지 여부</param>
        /// <returns>구독이 포함된 IDisposable 객체</returns>
        public IDisposable BindButtonInteractable(Button button, Bindable<bool> prop, bool refreshNow = true)
        {
            return BindInteractable(button, prop, refreshNow);
        }

        /// <summary>
        /// 여러 개의 IDisposable 객체를 관리하는 클래스입니다.
        /// </summary>
        public sealed class CompositeDisposable : IDisposable
        {
            /// <summary>
            /// <c>CompositeDisposable</c> 클래스 내부에서 사용되는 IDisposable 형식의 멤버 변수.
            /// 리소스 정리 시 초기화 및 해제를 담당합니다.
            /// </summary>
            private IDisposable _a, _b;

            /// <summary>
            /// 두 개의 IDisposable 객체를 관리하며, 함께 Dispose할 수 있도록 제공합니다.
            /// </summary>
            public CompositeDisposable(IDisposable a, IDisposable b)
            {
                _a = a;
                _b = b;
            }

            /// <summary>
            /// UguiBinder의 모든 바인딩 및 리소스를 해제합니다.
            /// 이 메서드를 호출하면 해당 객체가 더 이상 사용되지 않으므로 추가 작업 없이 정리됩니다.
            /// </summary>
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

                _a = null;
                _b = null;
            }
        }
    }
}