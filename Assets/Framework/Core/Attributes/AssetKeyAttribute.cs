using System;


namespace Core.Engine.Factory
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AssetKeyAttribute : Attribute
    {
        public readonly string Key;
        public AssetKeyAttribute(string key) => Key = key;
    }
}