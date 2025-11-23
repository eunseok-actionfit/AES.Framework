using UnityEngine;


namespace AES.Tools.Core
{
    [CreateAssetMenu(menuName="Game/Input Settings")]
    public sealed class InputConfig : ScriptableObject
    {
        [Header("General")]
        public bool blockWhenOverUI = true;
        [Tooltip("참조 DPI. 160(=mdpi) 기준 권장")]
        public float referenceDpi = 160f;

        [Header("Tap")]
        public bool useTap = true;
        public float tapMaxDuration = 0.25f;
        public float tapMaxMovePx = 20f;

        [Header("Double Tap")]
        public bool useDoubleTap = false;
        public float doubleTapMaxGap = 0.3f;
        public float doubleTapMaxMovePx = 24f;

        [Header("Long Press")]
        public bool useLongPress = false;
        public float longPressTime = 0.5f;
        public float longPressMoveTolerancePx = 15f;

        [Header("Swipe")]
        public bool useSwipe = false;
        public float swipeMinDistancePx = 60f;
        public float swipeMaxDuration = 0.6f;

        [Header("Pinch")]
        public bool usePinch = false;
        public float pinchMinDistanceDeltaPx = 5f;
        public float pinchMinFactorStep = 0.01f; // 배율 변화 스텝
    }
}