using UnityEngine;

public sealed class SceneTransitionUxRoot : MonoBehaviour
{
    [Header("Optional UX Components")]
    public TransitionUIScreen UI;
    public UGUICanvasGroupFader Fader;
    public UGUIInputBlocker InputBlocker;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}