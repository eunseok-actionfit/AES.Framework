using UnityEngine;
using UnityEngine.UI;

public sealed class TransitionUIScreen : MonoBehaviour, ITransitionUI
{
    public Text StatusText;
    public Button RetryButton;
    public Button ClearCacheButton;

    public void SetStatus(string messageKey)
    {
        StatusText.text = messageKey;
    }

    public void SetRetryVisible(bool visible) => RetryButton.gameObject.SetActive(visible);
    public void SetClearCacheVisible(bool visible) => ClearCacheButton.gameObject.SetActive(visible);

    public void BindRetry(System.Action onRetry)
    {
        RetryButton.onClick.RemoveAllListeners();
        RetryButton.onClick.AddListener(() => onRetry?.Invoke());
    }

    public void BindClearCache(System.Action onClearCache)
    {
        ClearCacheButton.onClick.RemoveAllListeners();
        ClearCacheButton.onClick.AddListener(() => onClearCache?.Invoke());
    }
}