using System;
using System.Threading;


namespace AES.Tools.Models
{
    public enum SceneLoadMode { Single, Additive }

    public readonly struct SceneLoadOptions
    {
        public readonly SceneLoadMode Mode;
        public readonly bool SetActive;
        public readonly bool AllowSceneActivation;
        public readonly Action<float> OnProgress;
        public readonly CancellationToken ExternalToken;
        
        public SceneLoadOptions(
            SceneLoadMode mode = SceneLoadMode.Additive,
            bool setActive = true,
            bool allowSceneActivation = true,
            Action<float> onProgress = null,
            CancellationToken externalToken = default)
        {
            Mode = mode;
            SetActive = setActive;
            AllowSceneActivation = allowSceneActivation;
            OnProgress = onProgress;
            ExternalToken = externalToken;
        }

        public SceneLoadOptions WithProgress(Action<float> cb) =>
            new(Mode, SetActive, AllowSceneActivation, cb, ExternalToken);
    }
}