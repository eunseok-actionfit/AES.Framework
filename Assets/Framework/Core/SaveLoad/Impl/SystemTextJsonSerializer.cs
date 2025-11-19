using Core.Systems.Storage.Core;
using Newtonsoft.Json;


namespace Core.Systems.Storage.Impl
{
    public sealed class NewtonsoftJsonSerializer : IJsonSerializer
    {
        readonly JsonSerializerSettings settings;

        public NewtonsoftJsonSerializer(JsonSerializerSettings settings = null)
        {
            this.settings = settings ?? new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include
            };
        }

        public string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, settings);
        }

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, settings);
        }
    }
}