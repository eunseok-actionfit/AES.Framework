using UnityEngine;

public sealed class SceneTransitionUxRoot : MonoBehaviour
{
    [Header("Optional UX Components (plug-in)")]
    [SerializeField] private TransitionUIScreen ui;
    
    [SerializeField] private FaderBase fader;        // must implement IFader
    [SerializeField] private MonoBehaviour inputBlocker; // must implement IInputBlocker

    public TransitionUIScreen UI => ui;
    public IFader Fader => fader;
    public IInputBlocker InputBlocker => inputBlocker as IInputBlocker;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

    }
}