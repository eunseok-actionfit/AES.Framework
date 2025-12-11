using UnityEngine;


public class LifeCycleDebug : MonoBehaviour
{
    private void OnApplicationFocus(bool focus)
    {
        Debug.Log($"[LifeCycleDebug] Focus = {focus}");
    }

    private void OnApplicationPause(bool pause)
    {
        Debug.Log($"[LifeCycleDebug] Pause = {pause}");
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[LifeCycleDebug] Quit");
    }
}