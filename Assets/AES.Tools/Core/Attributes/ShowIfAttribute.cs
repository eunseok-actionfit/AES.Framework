


using UnityEngine;


namespace AES.Tools
{
    public class ShowIfAttribute : PropertyAttribute
    {
        public readonly string ConditionFieldName;
        public ShowIfAttribute(string conditionFieldName)
        {
            ConditionFieldName = conditionFieldName;
        }
    }
}


