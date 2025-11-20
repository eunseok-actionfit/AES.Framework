// Systems/UI/Core/UILayer.cs
using AES.Tools.Components.Transitions.TransitionAsset;
using UnityEngine;
using UnityEngine.UI;


namespace AES.Tools.Core.UILayer
{
    public enum LayerSortingPolicy { ByZPriority, ByTime }

    [System.Serializable]
    public sealed class UILayerPolicy
    {
        [Header("Behavior")]
        public bool BlocksInput;
        public bool ModalStack;
        public LayerSortingPolicy SortingPolicy = LayerSortingPolicy.ByZPriority;

        [Header("Canvas Helpers")]
        public bool AutoBindCamera;
        public bool UseSafeArea;

        [Header("Blocker (optional)")]
        public Image InputBlocker;
        public Color DimColor = new Color(0, 0, 0, 0);

        [Header("Transitions")]
        public TransitionAsset BaseTransitionAsset;

        [Header("SafeArea Extra Insets")]
        public bool UseExtraInsets;
        public int ExtraBottomPx;
        public GameObject ExtraInsetsSourceObject;
    }

    [DisallowMultipleComponent]
    public sealed class UILayer : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform content;

        [Header("Policy")]
        public UILayerPolicy Policy = new();

        [Header("SafeArea Animation (optional)")]
        [SerializeField] private bool animateChanges;
        [SerializeField] private float animDuration = 0.25f;

        public Canvas Canvas => canvas;
        public RectTransform Content => content;

        // ─ 내부 상태 ─
        private Rect _lastSafeArea;
        private Vector2 _lastScreenSize;
        private bool _animating;
        private float _animT;
        private Vector2 _curMin, _curMax;
        private Vector2 _targetMin, _targetMax;
        private IExtraInsetsProvider _extraProviderCached;

        private void Reset()
        {
            canvas = GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            if (GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

            if (content == null)
            {
                var go = new GameObject("Content", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                content = go.GetComponent<RectTransform>();
                content.anchorMin = Vector2.zero;
                content.anchorMax = Vector2.one;
                content.offsetMin = Vector2.zero;
                content.offsetMax = Vector2.zero;
            }
        }

        private void Awake()
        {
            _lastSafeArea = Rect.zero;
            _lastScreenSize = new Vector2(Screen.width, Screen.height);
            CacheExtraProvider();
        }

        private void OnValidate() => CacheExtraProvider();

        private void CacheExtraProvider()
        {
            _extraProviderCached = null;

            var extraInsetsSourceObject = Policy?.ExtraInsetsSourceObject;
            if (extraInsetsSourceObject == null) return;
            extraInsetsSourceObject.TryGetComponent(out _extraProviderCached);
        }

        private void OnEnable() => TryApplySafeArea(force: true);

        private void Update()
        {
            TryApplySafeArea();
            TickAnchorAnimation();
        }

        public void EnsureCameraBound()
        {
            if (!Policy.AutoBindCamera || canvas == null) return;
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
                canvas.worldCamera = Camera.main;
        }

        // ────────────────────────────────────────────────────────────────────────────────
        public void ApplySafeArea()
        {
            if (!Policy.UseSafeArea || content == null) return;

            var safe = Screen.safeArea;
            var width = (float)Screen.width;
            var height = (float)Screen.height;
            if (width <= 0f || height <= 0f) return;

            if (Policy.UseExtraInsets)
            {
                var extraBottom = Mathf.Max(0, Policy.ExtraBottomPx);

                if (_extraProviderCached != null)
                {
                    var ro = _extraProviderCached.GetExtraInsets();
                    if (ro != null) extraBottom = Mathf.Max(extraBottom, ro.bottom);
                }

                if (extraBottom > 0)
                {
                    safe.y += extraBottom;
                    safe.height -= extraBottom;
                }
            }

            var min = new Vector2(safe.xMin / width, safe.yMin / height);
            var max = new Vector2(safe.xMax / width, safe.yMax / height);
            min = Clamp01(min);
            max = Clamp01(max);

            if (!animateChanges)
            {
                content.anchorMin = min;
                content.anchorMax = max;
                content.offsetMin = Vector2.zero;
                content.offsetMax = Vector2.zero;
            }
            else
            {
                _curMin = content.anchorMin;
                _curMax = content.anchorMax;
                _targetMin = min;
                _targetMax = max;
                _animT = 0f;
                _animating = true;
            }
        }

        public void ReapplySafeArea()
        {
            _lastScreenSize = Vector2.zero;
            TryApplySafeArea(force: true);
        }

        // ───────── Blocker 생성/제어 ─────────
        public void SetInputBlocker(bool on)
        {
            if (!Policy.BlocksInput) return;
            var img = EnsureBlocker(active: on);
            if (img == null) return;

            var needParent = Content ? Content : (RectTransform)transform;
            if (img.rectTransform.parent != needParent)
                img.rectTransform.SetParent(needParent, false);
        }

        public UIClickCatcher EnsureClickCatcherReady()
        {
            if (!Policy.BlocksInput) return null;
            var img = EnsureBlocker(active: false);
            return (img != null) ? img.GetComponent<UIClickCatcher>() : null;
        }

        private Image EnsureBlocker(bool active)
        {
            var parent = (RectTransform)(Content ? (Transform)Content : transform);

            if (Policy.InputBlocker == null)
            {
                var go = new GameObject("Blocker", typeof(RectTransform), typeof(Image), typeof(UIClickCatcher));
                go.transform.SetParent(parent, false);

                var r = (RectTransform)go.transform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;

                var img = go.GetComponent<Image>();
                img.color = Policy.DimColor;
                img.raycastTarget = true;

                Policy.InputBlocker = img;
            }
            else
            {
                if (Policy.InputBlocker.rectTransform.parent != parent)
                    Policy.InputBlocker.rectTransform.SetParent(parent, false);
            }

            Policy.InputBlocker.gameObject.SetActive(active);
            return Policy.InputBlocker;
        }

        /// <summary>명시적으로 top 아래로 옮길 때 호출.</summary>
        public void PlaceBlockerBelowTop(Transform topView)
        {
            if (!Policy.BlocksInput || Policy.InputBlocker == null || topView == null) return;

            var blocker = Policy.InputBlocker.rectTransform;
            if (blocker.parent != topView.parent)
                blocker.SetParent(topView.parent as RectTransform, worldPositionStays: false);

            var topIdx = topView.GetSiblingIndex() - 1;
            blocker.SetSiblingIndex(topIdx);
        }

        public UIClickCatcher GetClickCatcher()
            => (Policy.InputBlocker != null) ? Policy.InputBlocker.GetComponent<UIClickCatcher>() : null;

        // ─ 내부: SafeArea 감지 ─
        private void TryApplySafeArea(bool force = false)
        {
            if (!Policy.UseSafeArea || content == null) return;

            var curSafe = Screen.safeArea;
            var curSize = new Vector2(Screen.width, Screen.height);

            if (force || !Approximately(curSafe, _lastSafeArea) || curSize != _lastScreenSize)
            {
                _lastSafeArea = curSafe;
                _lastScreenSize = curSize;
                ApplySafeArea();
            }
        }

        private void TickAnchorAnimation()
        {
            if (!_animating || content == null) return;

            _animT += Time.unscaledDeltaTime / Mathf.Max(0.0001f, animDuration);
            var t = Mathf.Clamp01(_animT);
            t = 1f - (1f - t) * (1f - t); // easeOutQuad

            var nMin = Vector2.Lerp(_curMin, _targetMin, t);
            var nMax = Vector2.Lerp(_curMax, _targetMax, t);

            content.anchorMin = nMin;
            content.anchorMax = nMax;
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            if (t >= 1f) _animating = false;
        }

        private static Vector2 Clamp01(Vector2 v) => new(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y));

        private static bool Approximately(Rect a, Rect b)
        {
            const float eps = 0.5f;
            return Mathf.Abs(a.x - b.x) < eps &&
                   Mathf.Abs(a.y - b.y) < eps &&
                   Mathf.Abs(a.width - b.width) < eps &&
                   Mathf.Abs(a.height - b.height) < eps;
        }
    }
}