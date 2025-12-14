using UnityEngine;

namespace AES.Tools
{
    public sealed class AesFoldoutGroupAttribute : PropertyAttribute
    {
        public readonly string Name;
        public readonly bool DefaultExpanded;

        public AesFoldoutGroupAttribute(string name, bool defaultExpanded = true)
        {
            Name = name;
            DefaultExpanded = defaultExpanded;
        }
    }
}