using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public sealed class GraphicMaterialFader : FaderBase
{
    [Header("Target")]
    [SerializeField] private Graphic target;

    [Header("Material Property")]
    [SerializeField] private string fadeProperty = "_Progress";
    [SerializeField] private bool instantiateMaterial = true;

    [Header("Fade Values")]
    [SerializeField] private float fadeInValue = 1f;
    [SerializeField] private float fadeOutValue = 0f;

    [Header("Curve")]
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Toggle Graphic")]
    [SerializeField] private bool disableGraphicOnFadeOut = true;
    [SerializeField] private bool enableGraphicOnFadeIn = true;
    [SerializeField] private bool disableRaycastOnFadeOut = true;
    [SerializeField] private float disableThreshold = 0.0001f;

    private int _fadeId;
    private Material _mat;

    // 추가: 마지막으로 셋한 값 캐시
    private float _currentValue;

    // 추가: 중복 페이드 취소용
    private CancellationTokenSource _localCts;

    private void Awake()
    {
        if (!target) target = GetComponentInChildren<Graphic>(true);
        if (string.IsNullOrWhiteSpace(fadeProperty)) fadeProperty = "_Progress";
        _fadeId = Shader.PropertyToID(fadeProperty);

        if (!target) return;

        if (instantiateMaterial)
        {
            _mat = Instantiate(target.material);
            target.material = _mat;
        }
        else
        {
            _mat = target.material;
        }

        _currentValue = fadeOutValue;
        Set(_currentValue);
        ApplyPostState(_currentValue, isFadeIn: false);
    }

    public override UniTask FadeIn(float duration, CancellationToken ct)  => FadeToGuarded(fadeInValue, duration, ct, isFadeIn: true);
    public override UniTask FadeOut(float duration, CancellationToken ct) => FadeToGuarded(fadeOutValue, duration, ct, isFadeIn: false);

    private UniTask FadeToGuarded(float targetValue, float duration, CancellationToken externalCt, bool isFadeIn)
    {
        _localCts?.Cancel();
        _localCts?.Dispose();
        _localCts = new CancellationTokenSource();

        // 외부 ct + 내부 ct 결합
        var linked = CancellationTokenSource.CreateLinkedTokenSource(externalCt, _localCts.Token);
        return FadeTo(targetValue, duration, linked.Token, isFadeIn);
    }

    private async UniTask FadeTo(float targetValue, float duration, CancellationToken ct, bool isFadeIn)
    {
        if (!_mat || !target) return;

        if (isFadeIn)
        {
            if (enableGraphicOnFadeIn) target.enabled = true;
            if (disableRaycastOnFadeOut) target.raycastTarget = true;
        }

        // 핵심: start는 GetFloat 대신 캐시 사용
        float start = _currentValue;

        if (duration <= 0f)
        {
            _currentValue = targetValue;
            _mat.SetFloat(_fadeId, _currentValue);
            ApplyPostState(_currentValue, isFadeIn);
            return;
        }

        float t = 0f;
        while (t < duration)
        {
            ct.ThrowIfCancellationRequested();
            
            t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

            float n = Mathf.Clamp01(t / duration);
            float eased = curve.Evaluate(n);
            float v = Mathf.LerpUnclamped(start, targetValue, eased);

            _currentValue = v;
            _mat.SetFloat(_fadeId, _currentValue);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }


        _currentValue = targetValue;
        _mat.SetFloat(_fadeId, _currentValue);
        ApplyPostState(_currentValue, isFadeIn);
    }

    private void ApplyPostState(float value, bool isFadeIn)
    {
        if (!target) return;

        bool isFullyOff = value <= disableThreshold;

        if (!isFadeIn)
        {
            if (disableRaycastOnFadeOut && isFullyOff) target.raycastTarget = false;
            if (disableGraphicOnFadeOut && isFullyOff) target.enabled = false;
        }
        else
        {
            if (enableGraphicOnFadeIn) target.enabled = true;
            if (disableRaycastOnFadeOut) target.raycastTarget = true;
        }
    }

    private void Set(float v)
    {
        _currentValue = v;
        if (_mat) _mat.SetFloat(_fadeId, _currentValue);
    }

    private void OnDestroy()
    {
        _localCts?.Cancel();
        _localCts?.Dispose();

        if (instantiateMaterial && _mat)
            Destroy(_mat);
    }
}
