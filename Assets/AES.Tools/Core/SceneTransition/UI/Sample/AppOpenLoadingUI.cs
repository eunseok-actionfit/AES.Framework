using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class AppOpenLoadingUI : LoadingOverlayUIBase
{
    [SerializeField] private Slider bar;
    [SerializeField] private TMP_Text label;

    public override void SetProgress(float realtime01, float smoothed01)
    {
        Debug.Log($"AppOpen Loading: {smoothed01}");
        if (bar) bar.value = Mathf.Clamp01(smoothed01);
    }

    public override void SetMessage(string message)
    {
        if (label) label.text = message ?? "";
    }
}