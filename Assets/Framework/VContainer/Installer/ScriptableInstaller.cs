using UnityEngine;
using VContainer;

public abstract class ScriptableInstaller : ScriptableObject
{
    public abstract void Install(IContainerBuilder builder);
}