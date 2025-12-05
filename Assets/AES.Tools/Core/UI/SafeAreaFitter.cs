using UnityEngine;

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

    void Awake()
    {
        _rect = GetComponent<RectTransform>();

        // 인스펙터에서 padding 비워놨을 때 NRE 방지
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
        // 에디터는 GameView 변경을 바로 보고 싶은 경우가 많으니 매 프레임 체크
        CheckAndApply();
#else
        const float POLL_INTERVAL = 0.2f;

        // 모바일/런타임은 폴링 주기에 맞춰 체크
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

        // SafeArea / 해상도 / 방향이 그대로면 스킵
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

        // --- SafeArea + padding 보정 ---

        var adjusted = safe;

        if (padding != null)
        {
            adjusted.xMin += padding.left;
            adjusted.xMax -= padding.right;
            adjusted.yMin += padding.bottom;
            adjusted.yMax -= padding.top;
        }

        // 화면 경계를 넘지 않도록 clamp
        adjusted.xMin = Mathf.Clamp(adjusted.xMin, 0f, w);
        adjusted.xMax = Mathf.Clamp(adjusted.xMax, 0f, w);
        adjusted.yMin = Mathf.Clamp(adjusted.yMin, 0f, h);
        adjusted.yMax = Mathf.Clamp(adjusted.yMax, 0f, h);

        if (adjusted.width <= 0f || adjusted.height <= 0f)
        {
            // padding/보정으로 SafeArea가 말려버리면 전체 화면 사용
            adjusted = new Rect(0f, 0f, w, h);
        }

        // --- Rect → Anchor 비율로 변환 ---

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

    // 배너 광고 예시
    // public void OnBannerLoaded(int bannerHeightPx)
    // {
    //     padding.bottom = bannerHeightPx;
    //     ForceApply();
    // }
    //
    // public void OnBannerHidden()
    // {
    //     padding.bottom = 0;
    //     ForceApply();
    // }
}
