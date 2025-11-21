namespace AES.Tools.Models
{
    // 이름 기반 로딩(비어 있지 않은 sceneName) 또는 Addressables 키 기반 로딩(비어 있지 않은 addressKey)
    public readonly struct SceneRef
    {
        public readonly string SceneName;   // Build Settings 이름형
        public readonly string AddressKey;  // Addressables 키형

        public bool IsAddressable => !string.IsNullOrEmpty(AddressKey);
        public bool IsNamed       => !string.IsNullOrEmpty(SceneName);

        public SceneRef(string sceneName = null, string addressKey = null)
        {
            SceneName = sceneName;
            AddressKey = addressKey;
        }

        public override string ToString() => IsAddressable ? AddressKey : SceneName;
    }
}