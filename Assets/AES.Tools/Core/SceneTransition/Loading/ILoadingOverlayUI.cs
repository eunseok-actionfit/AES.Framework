public interface ILoadingOverlayUI
{
    void SetProgress(float realtime01, float smoothed01);
    void SetMessage(string message);
}