public interface ISceneArgsCarrier
{
    void Set<T>(T args) where T : class, ISceneArgs;
    bool TryConsume<T>(out T args) where T : class, ISceneArgs;
    void Clear();
}