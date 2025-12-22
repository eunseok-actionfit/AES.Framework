public readonly struct LoadingScreenKey
{
    public readonly string UnityKey;        // name/path
    public readonly string AddressablesKey; // address
    public readonly bool IsAddressable;

    public LoadingScreenKey(string unityKey, string addrKey, bool isAddr)
    {
        UnityKey = unityKey;
        AddressablesKey = addrKey;
        IsAddressable = isAddr;
    }
}