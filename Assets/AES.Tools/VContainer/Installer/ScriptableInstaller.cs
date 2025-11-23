using UnityEngine;
using VContainer;


namespace AES.Tools.VContainer.Installer
{
    public abstract class ScriptableInstaller : ScriptableObject
    {
        public abstract void Install(IContainerBuilder builder);
    }
}