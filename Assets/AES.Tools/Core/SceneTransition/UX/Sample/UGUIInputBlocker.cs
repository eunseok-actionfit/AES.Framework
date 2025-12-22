using UnityEngine;


public sealed class UGUIInputBlocker : MonoBehaviour, IInputBlocker
{
    [Header("Full-screen Image or Panel (Raycast Target ON)")]
    public GameObject BlockPanel;

    private void Awake()
    {
        if (BlockPanel != null) BlockPanel.SetActive(false);
    }

    public void Block()
    {
        if (BlockPanel != null) BlockPanel.SetActive(true);
    }

    public void Unblock()
    {
        if (BlockPanel != null) BlockPanel.SetActive(false);
    }
}