public static class CacheKeyResolver
{
    public static object ResolveForCacheKey(string sceneKeyOrName, string preferredLabel = null)
    {
        if (!string.IsNullOrEmpty(preferredLabel))
            return preferredLabel;

        return sceneKeyOrName;
    }
}