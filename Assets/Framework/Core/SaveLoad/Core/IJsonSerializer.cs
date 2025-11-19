namespace Core.Systems.Storage.Core {
    public interface IJsonSerializer {
        string Serialize<T>(T obj);
        T Deserialize<T>(string json);
    }
}