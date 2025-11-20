namespace AES.Tools.Core {
    public interface IJsonSerializer {
        string Serialize<T>(T obj);
        T Deserialize<T>(string json);
    }
}