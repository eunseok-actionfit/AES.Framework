using UnityEngine;
using AES.Tools;
using AES.Tools.VContainer;


public sealed class AdsLoadingOverlay : MonoBehaviour
{
    [Header("광고 중에만 보여줄 오버레이 패널")]
    [SerializeField] private GameObject overlayRoot;

    private EventBinding<AdShowingStateChangedEvent> _binding;
    private int _showCount; // 중첩 방지용

    private void Awake()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        _binding = new EventBinding<AdShowingStateChangedEvent>()
            .Add(OnAdStateChanged)
            .Register();
    }

    private void OnDestroy()
    {
        _binding?.Deregister();
        _binding = null;
    }

    private void OnAdStateChanged(AdShowingStateChangedEvent e)
    {
        if (e.IsShowing)
            _showCount++;
        else
            _showCount = Mathf.Max(0, _showCount - 1);

        bool active = _showCount > 0;

        if (overlayRoot != null)
            overlayRoot.SetActive(active);
    }
}