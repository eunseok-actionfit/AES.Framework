public readonly struct ResolvedSceneKey
{
    public readonly string ForUnity;        // path or name
    public readonly string ForAddressables; // address
    public readonly bool IsAddressable;

    public ResolvedSceneKey(string forUnity, string forAddressables, bool isAddressable)
    {
        ForUnity = forUnity;
        ForAddressables = forAddressables;
        IsAddressable = isAddressable;
    }
}