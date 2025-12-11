using UnityEngine;

namespace AES.Tools
{
    public abstract class AesConditionAttributeBase : PropertyAttribute
    {
        public readonly string Condition;
        public readonly object CompareValue;
        public readonly bool HasCompareValue;
        public readonly bool IsExpression;

        protected AesConditionAttributeBase(string condition)
        {
            Condition = condition;
            IsExpression = !string.IsNullOrEmpty(condition) && condition[0] == '@';
            HasCompareValue = false;
        }

        protected AesConditionAttributeBase(string condition, object compareValue)
        {
            Condition = condition;
            IsExpression = !string.IsNullOrEmpty(condition) && condition[0] == '@';
            CompareValue = compareValue;
            HasCompareValue = true;
        }
    }
}