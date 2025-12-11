using UnityEngine;
using AES.Tools;
using AES.Tools.UI;


[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    [Header("Which sides use SafeArea?")]
    public bool applyLeft   = true;
    public bool applyRight  = true;
    public bool applyTop    = true;
    public bool applyBottom = true;

    [Header("Extra Padding (inside SafeArea, in pixels)")]
    [Tooltip("SafeArea 안쪽에서 한 번 더 깎고 싶은 패딩 (px 기준)")]
    public RectOffset padding;

    RectTransform _rect;

    Rect _lastSafeArea;
    Vector2Int _lastResolution;
    ScreenOrientation _lastOrientation;
    float _timer;

    bool _isApplying;
    
    EventBinding<BannerHeightChangedEvent> _bannerBinding;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();

        if (padding == null)
            padding = new RectOffset(0, 0, 0, 0);
    }

    void OnEnable()
    {
        ForceApply();

        // ★ 배너 높이 변경 이벤트 구독
        _bannerBinding = new EventBinding<BannerHeightChangedEvent>()
            .Add(OnBannerEvent)
            .Register();
    }

    void OnDisable()
    {
        _bannerBinding?.Deregister();
        _bannerBinding = null;
    }

    void OnBannerEvent(BannerHeightChangedEvent e)
    {
        // 배너가 보이면 px만큼 bottom 패딩, 숨기면 0
        padding.bottom = e.Visible ? e.HeightPx : 0;
        ForceApply();
    }

    void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
            return;

        CheckAndApply();
    }

    void Update()
    {
#if UNITY_EDITOR
        CheckAndApply();
#else
        const float POLL_INTERVAL = 0.2f;

        _timer += Time.unscaledDeltaTime;
        if (_timer >= POLL_INTERVAL)
        {
            _timer = 0f;
            CheckAndApply();
        }
#endif
    }

    void CheckAndApply()
    {
        if (_isApplying)
            return;

        var safe = Screen.safeArea;
        var res  = new Vector2Int(Screen.width, Screen.height);
        var ori  = Screen.orientation;

        if (safe == _lastSafeArea &&
            res  == _lastResolution &&
            ori  == _lastOrientation)
            return;

        _isApplying = true;
        ApplyInternal(safe, res, ori);
        _isApplying = false;
    }

    public void ForceApply()
    {
        _isApplying = true;
        ApplyInternal(
            Screen.safeArea,
            new Vector2Int(Screen.width, Screen.height),
            Screen.orientation
        );
        _isApplying = false;
    }

    void ApplyInternal(Rect safe, Vector2Int resolution, ScreenOrientation orientation)
    {
        if (_rect == null)
            _rect = GetComponent<RectTransform>();

        float w = resolution.x;
        float h = resolution.y;

        if (w <= 0f || h <= 0f)
            return;

        var adjusted = safe;

        if (padding != null)
        {
            adjusted.xMin += padding.left;
            adjusted.xMax -= padding.right;
            adjusted.yMin += padding.bottom;
            adjusted.yMax -= padding.top;
        }

        adjusted.xMin = Mathf.Clamp(adjusted.xMin, 0f, w);
        adjusted.xMax = Mathf.Clamp(adjusted.xMax, 0f, w);
        adjusted.yMin = Mathf.Clamp(adjusted.yMin, 0f, h);
        adjusted.yMax = Mathf.Clamp(adjusted.yMax, 0f, h);

        if (adjusted.width <= 0f || adjusted.height <= 0f)
        {
            adjusted = new Rect(0f, 0f, w, h);
        }

        Vector2 anchorMin = adjusted.position;
        Vector2 anchorMax = adjusted.position + adjusted.size;

        anchorMin.x /= w;
        anchorMin.y /= h;
        anchorMax.x /= w;
        anchorMax.y /= h;

        Vector2 finalMin = _rect.anchorMin;
        Vector2 finalMax = _rect.anchorMax;

        if (applyLeft)   finalMin.x = anchorMin.x;
        if (applyBottom) finalMin.y = anchorMin.y;
        if (applyRight)  finalMax.x = anchorMax.x;
        if (applyTop)    finalMax.y = anchorMax.y;

        _rect.anchorMin = finalMin;
        _rect.anchorMax = finalMax;

        _lastSafeArea    = safe;
        _lastResolution  = resolution;
        _lastOrientation = orientation;
    }
}
