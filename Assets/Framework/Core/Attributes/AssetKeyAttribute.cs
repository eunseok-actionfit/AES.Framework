using System;


namespace AES.Tools
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AssetKeyAttribute : Attribute
    {
        public readonly string Key;
        public AssetKeyAttribute(string key) => Key = key;
    }
}