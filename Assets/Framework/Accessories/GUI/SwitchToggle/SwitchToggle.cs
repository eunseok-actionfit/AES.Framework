using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Core.Systems.UI.Components.Controls
{
    [RequireComponent(typeof(Toggle))]
    public class SwitchToggle : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private RectTransform handle;       // 36x36
        [SerializeField] private Image         fill;         // ON 색상 레이어
        [SerializeField] private TextMeshProUGUI labelOn;
        [SerializeField] private TextMeshProUGUI labelOff;

        [Header("Layout")]
        [SerializeField] private float handleLeftX  = -20f;
        [SerializeField] private float handleRightX =  20f;

        [Header("Anim / Handle Move")]
        [SerializeField] private float duration  = 0.18f;      // 기본 이징 시간
        [Range(0f, 1f)]
        [SerializeField] private float          overshoot = 1.08f;      // >1이면 살짝 튕김
        [SerializeField] private AnimationCurve ease      = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Anim / Handle Scale")]
        [SerializeField] private float baseScale       = 1.00f; // 기본
        [SerializeField] private float pressScale      = 1.08f; // 초반 '확' 커짐
        [SerializeField] private float midDipScale     = 0.96f; // 이동 중 살짝 작아짐
        [SerializeField] private float arriveOvershoot = 1.04f; // 도착 직후 오버슛

        [Header("Colors/Alpha")]
        [SerializeField] private Color onFillColor = Color.white;
        [SerializeField] private Color offFillColor = new Color(1,1,1,0.25f);

        [SerializeField, HideInInspector] private Toggle toggle;
        private CancellationTokenSource _cts;
        private RectTransform _labelsRt;

        private void Awake()
        {
            TryGetComponent(out toggle);

            // labelOn/labelOff의 공통 부모 RectTransform 캐시
            if (labelOn != null)
                _labelsRt = (RectTransform)labelOn.transform.parent;
            else if (labelOff != null)
                _labelsRt = (RectTransform)labelOff.transform.parent;
        }

        private void OnEnable()
        {
            toggle.onValueChanged.AddListener(OnToggleChanged);
            ApplyImmediateState(toggle.isOn); // 초기 상태 반영
        }

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
            CancelAndDisposeCts();
        }

        private void OnValidate()
        {
            // 에디터에서 값 변경 시 즉시 반영(재생 중이 아닐 때만)
#if UNITY_EDITOR
            if (Application.isPlaying) return;
            if (toggle == null) 
                toggle = GetComponent<Toggle>();
            if (fill != null)
                ApplyImmediateState(toggle != null && toggle.isOn);
#endif
        }

        private void OnToggleChanged(bool isOn)
        {
            CancelAndDisposeCts();
            _cts = new CancellationTokenSource();
            _ = PlayAnimationAsync(isOn, _cts.Token);
        }

        private void ApplyImmediateState(bool isOn)
        {
            float x = isOn ? handleRightX : handleLeftX;

            // 핸들 위치
            if (handle != null)
            {
                var pos = handle.anchoredPosition;
                pos.x = x;
                pos.y = 0f;
                handle.anchoredPosition = pos;
                SetHandleScale(baseScale);
            }

            // Fill
            if (fill != null)
                fill.color = isOn ? onFillColor : offFillColor;

            // Labels
            ApplyLabelState(isOn);
            AlignLabelsToHandle(x);
        }

        private async UniTask PlayAnimationAsync(bool turningOn, CancellationToken ct)
        {
            if (handle == null) return;

            float startX = handle.anchoredPosition.x;
            float endX   = turningOn ? handleRightX : handleLeftX;

            // 오버슈트 위치 계산
            float overshootX = Mathf.Lerp(endX, endX + Mathf.Sign(endX - startX) * 4f, overshoot);

            // 구간 1: 메인 이징
            float t = 0f;
            while (t < 1f)
            {
                ct.ThrowIfCancellationRequested();
                t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);

                float e = ease.Evaluate(Mathf.Clamp01(t));
                float x = Mathf.Lerp(startX, overshootX, e);

                // 스케일(진행도 기반)
                SetHandleScale(EvalMainScale(Mathf.Clamp01(t)));

                UpdateFrame(x, turningOn ? e : 1f - e);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            // 구간 2: 오버슈트 복귀 + 도착 직후 오버슛 감쇠
            float backDur = duration * 0.35f;
            float t2 = 0f;

            while (t2 < 1f)
            {
                ct.ThrowIfCancellationRequested();
                t2 += Time.unscaledDeltaTime / Mathf.Max(0.0001f, backDur);

                // easeOutQuad
                float e2 = 1f - Mathf.Pow(1f - Mathf.Clamp01(t2), 2f);
                float x = Mathf.Lerp(overshootX, endX, e2);

                // 스케일: arriveOvershoot → baseScale
                SetHandleScale(Mathf.Lerp(arriveOvershoot, baseScale, e2));

                UpdateFrame(x, turningOn ? 1f : 0f);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            // 마지막 스냅
            ApplyImmediateState(turningOn);
        }

        private void UpdateFrame(float handleX, float onFactor01)
        {
            // 핸들 위치
            if (handle != null)
            {
                var pos = handle.anchoredPosition;
                pos.x = handleX;
                pos.y = 0f;
                handle.anchoredPosition = pos;
            }

            // Fill 색상 보간
            if (fill != null)
                fill.color = Color.Lerp(offFillColor, onFillColor, Mathf.Clamp01(onFactor01));

            // 라벨 강조/페이드
            ApplyLabelState(onFactor01 >= 0.5f);

            // 라벨 정렬
            AlignLabelsToHandle(handleX);
        }

        private void ApplyLabelState(bool isOn)
        {
            if (labelOn != null)  labelOn.gameObject.SetActive(isOn);
            if (labelOff != null) labelOff.gameObject.SetActive(!isOn);
        }

        private void AlignLabelsToHandle(float handleX)
        {
            if (_labelsRt == null) return;

            float norm = Mathf.InverseLerp(handleLeftX, handleRightX, handleX); // 0~1

            // 좌/우 패딩 가변
            float leftPad  = Mathf.Lerp(12f, 4f,  norm);  // ON 쪽으로 갈수록 왼쪽 패딩 감소
            float rightPad = Mathf.Lerp(4f,  12f, norm);  // OFF 쪽 여백 증가

            var min = _labelsRt.offsetMin;
            var max = _labelsRt.offsetMax;
            _labelsRt.offsetMin = new Vector2(leftPad,  min.y);
            _labelsRt.offsetMax = new Vector2(-rightPad, max.y);
        }

        private void SetHandleScale(float s)
        {
            if (handle != null)
                handle.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>
        /// 0~1: 0~0.2(커짐) → 0.2~0.8(작아짐) → 0.8~1.0(기본으로 복귀)
        /// </summary>
        private float EvalMainScale(float t01)
        {
            t01 = Mathf.Clamp01(t01);

            if (t01 < 0.2f)                 // 커짐
                return Mathf.Lerp(baseScale, pressScale, t01 / 0.2f);

            if (t01 < 0.8f)                 // 중간 딥
                return Mathf.Lerp(pressScale, midDipScale, (t01 - 0.2f) / 0.6f);

            // 기본으로 복귀
            return Mathf.Lerp(midDipScale, baseScale, (t01 - 0.8f) / 0.2f);
        }

        private void CancelAndDisposeCts()
        {
            if (_cts == null) return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }
}
