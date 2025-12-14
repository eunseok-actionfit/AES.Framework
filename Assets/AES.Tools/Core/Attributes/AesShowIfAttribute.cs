// using System;
//
//
// namespace AES.Tools
// {
//     [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
//     public class AesShowIfAttribute : AesConditionAttributeBase
//     {
//         public AesShowIfAttribute(string condition) : base(condition) { }
//         public AesShowIfAttribute(string condition, object compareValue) : base(condition, compareValue) { }
//     }
//
//     [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
//     public class AesHideIfAttribute : AesConditionAttributeBase
//     {
//         public AesHideIfAttribute(string condition) : base(condition) { }
//         public AesHideIfAttribute(string condition, object compareValue) : base(condition, compareValue) { }
//     }
//
//     [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
//     public class AesEnableIfAttribute : AesConditionAttributeBase
//     {
//         public AesEnableIfAttribute(string condition) : base(condition) { }
//         public AesEnableIfAttribute(string condition, object compareValue) : base(condition, compareValue) { }
//     }
//
//     [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
//     public class AesDisableIfAttribute : AesConditionAttributeBase
//     {
//         public AesDisableIfAttribute(string condition) : base(condition) { }
//         public AesDisableIfAttribute(string condition, object compareValue) : base(condition, compareValue) { }
//     }
// }