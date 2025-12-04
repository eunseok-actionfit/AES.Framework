using UnityEngine;


[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    [Header("Which sides use SafeArea?")]
    public bool applyLeft = true;
    public bool applyRight = true;
    public bool applyTop = true;
    public bool applyBottom = true;

    [Header("Extra Padding (inside SafeArea, in pixels)")]
    [Tooltip("SafeArea 안쪽에서 한 번 더 깎고 싶은 패딩 (px 기준)")]
    public RectOffset padding; 

    RectTransform _rect;

    Rect _lastSafeArea;
    Vector2Int _lastResolution;
    ScreenOrientation _lastOrientation;
    float _timer;

    void Awake()
    {
        _rect = GetComponent<RectTransform>();

        if (padding == null)
            padding = new RectOffset(0, 0, 0, 0);
    }

    void OnEnable()
    {
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
        var safe = Screen.safeArea;
        var res = new Vector2Int(Screen.width, Screen.height);
        var ori = Screen.orientation;

        // SafeArea / 해상도 / 방향이 안 바뀐 경우에만 스킵
        if (safe == _lastSafeArea &&
            res == _lastResolution &&
            ori == _lastOrientation)
            return;

        ApplyInternal(safe, res, ori);
    }

    public void ForceApply()
    {
        ApplyInternal(
            Screen.safeArea,
            new Vector2Int(Screen.width, Screen.height),
            Screen.orientation
        );
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

        if (adjusted.width <= 0f || adjusted.height <= 0f) { adjusted = new Rect(0f, 0f, w, h); }

        Vector2 anchorMin = adjusted.position;
        Vector2 anchorMax = adjusted.position + adjusted.size;

        anchorMin.x /= w;
        anchorMin.y /= h;
        anchorMax.x /= w;
        anchorMax.y /= h;

        Vector2 finalMin = _rect.anchorMin;
        Vector2 finalMax = _rect.anchorMax;

        if (applyLeft) finalMin.x = anchorMin.x;
        if (applyBottom) finalMin.y = anchorMin.y;
        if (applyRight) finalMax.x = anchorMax.x;
        if (applyTop) finalMax.y = anchorMax.y;

        _rect.anchorMin = finalMin;
        _rect.anchorMax = finalMax;
        _rect.offsetMin = Vector2.zero; // SafeArea 루트라면 이게 더 안전
        _rect.offsetMax = Vector2.zero;

        _lastSafeArea = safe;
        _lastResolution = resolution;
        _lastOrientation = orientation;
    }

    // 배너 광고 예시
    // void OnBannerLoaded(int bannerHeightPx)
    // {
    //     padding.bottom = bannerHeightPx;
    //     ForceApply();
    // }
    //
    // void OnBannerHidden()
    // {
    //     padding.bottom = 0;
    //     ForceApply();
    // }
}