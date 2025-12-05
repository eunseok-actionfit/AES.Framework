using UnityEngine;
using UnityEngine.Events;

public class SimpleToggle : MonoBehaviour
{
    [Header("Initial State")]
    [SerializeField] private bool startOn = false;

    public bool IsOn { get; private set; }

    [Header("Events")]
    public UnityEvent onTurnOn = new UnityEvent();
    public UnityEvent onTurnOff = new UnityEvent();

    protected virtual void Awake()
    {
        IsOn = startOn;
    }

    public void Toggle() => SetState(!IsOn);

    public void TurnOn() => SetState(true);

    public void TurnOff() => SetState(false);

    private void SetState(bool value)
    {
        if (IsOn == value) return;

        IsOn = value;

        if (IsOn) onTurnOn?.Invoke();
        else      onTurnOff?.Invoke();
    }
}