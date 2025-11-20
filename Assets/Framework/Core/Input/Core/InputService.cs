using System;
using AES.Tools.Core.Utils.Systems.Input.Utils;
using AES.Tools.Gesture;
using AES.Tools.Platform;
using UnityEngine;


namespace AES.Tools.Core
{
    public sealed class InputService : IInputService
    {
        public bool Enabled { get; set; } = true;

        public bool IsPressed { get; private set; }
        public Vector2 Position { get; private set; }
        public Vector2 Delta { get; private set; }

        public event Action<Vector2> OnTap = delegate { };
        public event Action<Vector2> OnDoubleTap = delegate { };
        public event Action<Vector2, float> OnLongPress = delegate { };
        public event Action<Vector2, Vector2> OnSwipe = delegate { };
        public event Action<Vector2, float> OnPinch = delegate { };
        
        public event Action<Vector2> OnPointerDown= delegate { };
        public event Action<Vector2, Vector2> OnPointerDrag = delegate { };
        public event Action<Vector2> OnPointerUp= delegate { };


        private readonly InputConfig config;
        private readonly IPointerSource _pointer;

        // 제스처
        private readonly TapGesture _tap;
        private readonly DoubleTapGesture _double;
        private readonly HoldGesture _hold;
        private readonly SwipeGesture _swipe;
        private readonly PinchGesture _pinch;

        private Vector2 _prevPos;
        private int _activeId = -999;

        public InputService(InputConfig config, IPointerSource pointer)
        {
            this.config = config;
            _pointer = pointer;

            // DPI 보정
            float Px(float v) => DpiScaler.PxToDevice(v, this.config.referenceDpi);

            if (this.config.useTap)
                _tap = new TapGesture(Px(this.config.tapMaxMovePx), this.config.tapMaxDuration);

            if (this.config.useDoubleTap) {
                _double = new DoubleTapGesture(this.config.doubleTapMaxGap, Px(this.config.doubleTapMaxMovePx));
                _double.OnDoubleTap += p => OnDoubleTap.Invoke(p);
            }

            if (this.config.useLongPress)
                _hold = new HoldGesture(this.config.longPressTime, Px(this.config.longPressMoveTolerancePx));

            if (this.config.useSwipe)
                _swipe = new SwipeGesture(Px(this.config.swipeMinDistancePx), this.config.swipeMaxDuration);

            if (this.config.usePinch)
                _pinch = new PinchGesture(Px(this.config.pinchMinDistanceDeltaPx), this.config.pinchMinFactorStep);

            UnityEngine.Input.simulateMouseWithTouches = true;
        }

        public void Tick(float dt)
        {
            if (!Enabled) return;
            // 멀티터치 핀치
            if (config.usePinch && _pointer.TouchCount >= 2 && _pointer.TryGetTwoTouches(out var a, out var b)) {
                if (_activeId == -999) _pinch.Begin(a.position, b.position);
                if (_pinch.Update(a.position, b.position, out var center, out var factor))
                    OnPinch.Invoke(center, factor);
            }
            else if (config.usePinch && _pinch != null) { _pinch.End(); }

            // 단일 포인터
            if (_pointer.TryGetDown(out var down)) {
                if (!config.blockWhenOverUI || !UiBlocker.IsPointerOverUI(down.id)) { 
                    IsPressed = true;
                    _activeId = down.id;
                    Position = down.position;
                    Delta = Vector2.zero;
                    _prevPos = Position;

                    if (config.useTap) _tap.Begin(Position);
                    if (config.useLongPress) _hold.Begin(Position);
                    if (config.useSwipe) _swipe.Begin(Position);
                    
                    OnPointerDown.Invoke(Position);
                }
                else { CancelAll(); }
            }
            else if (_pointer.TryGetHold(out var hold) && IsPressed) {
                Position = hold.position;
                Delta = Position - _prevPos;
                _prevPos = Position;

                if (config.useTap) _tap.Update(dt);
                if (config.useSwipe) _swipe.Update(dt);
                if (config.useLongPress && _hold.Update(Position, dt, out var hp, out var dur))
                    OnLongPress.Invoke(hp, dur);
                if (config.useDoubleTap) _double.Update(dt);
                
                if (Delta.sqrMagnitude > 0f)
                    OnPointerDrag.Invoke(Position, Delta);
            }
            else if (_pointer.TryGetUp(out var up) && IsPressed) {
                Position = up.position;
                Delta = Position - _prevPos;
                _prevPos = Position;
                IsPressed = false;
                _activeId = -999;

                bool tapFired = false;
                if (config.useTap && _tap.TryEnd(Position)) {
                    tapFired = true;
                    OnTap.Invoke(Position);
                    if (config.useDoubleTap) _double.OnTap(Position); 
                }

                if (!tapFired && config.useSwipe && _swipe.TryEnd(Position, out var from, out var to))
                    OnSwipe.Invoke(from, to); 

                if (config.useLongPress) _hold.End();
                
                OnPointerUp.Invoke(Position);
            }
        }

        private void CancelAll()
        {
            IsPressed = false;
            _activeId = -999;
            _tap?.Cancel();
            _hold?.Cancel();
            _swipe?.Cancel();
            _pinch?.Cancel();
        }
    }
}