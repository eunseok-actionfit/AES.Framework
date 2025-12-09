using System;
using UnityEngine;

namespace AES.Tools
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class AesFoldoutGroupAttribute : PropertyAttribute
    {
        public readonly string GroupName;

        public AesFoldoutGroupAttribute(string groupName)
        {
            GroupName = groupName;
        }
    }
}