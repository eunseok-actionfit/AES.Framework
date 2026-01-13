using UnityEngine;


public sealed class SceneArgsCarrier : ISceneArgsCarrier
{
    private object _value;

    public void Set<T>(T args) where T : class, ISceneArgs
    {
        _value = args;
        Debug.Log(args);
    }

    public bool TryConsume<T>(out T args) where T : class, ISceneArgs
    {
        if (_value is T t)
        {
            args = t;
            _value = null; // 1회 소비
            return true;
        }

        args = null;
        return false;
    }

    public void Clear() => _value = null;
}