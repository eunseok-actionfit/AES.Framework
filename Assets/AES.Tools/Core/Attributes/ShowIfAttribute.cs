using System;
using System.Diagnostics;
using UnityEngine;

namespace AES.Tools
{
    public enum ShowIfCondition
    {
        BoolIsTrue,   // 기본: bool 필드가 true일 때 표시
        BoolIsFalse,  // 필요하면 사용

        Equals,       // 단일 값 == 비교
        NotEquals,    // 단일 값 != 비교

        In,           // 여러 값 중 하나일 때
        NotIn         // 여러 값 모두 아닐 때
    }
    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    [Conditional("UNITY_EDITOR")]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string ConditionFieldName { get; }
        public ShowIfCondition Condition { get; set; }

        // enum, 숫자 등 비교에 쓸 원본 값들
        public object[] RawValues { get; }

        // bool 전용: [ShowIf("someBool")]
        public ShowIfAttribute(string conditionFieldName)
        {
            ConditionFieldName = conditionFieldName;
            Condition = ShowIfCondition.BoolIsTrue;
            RawValues = Array.Empty<object>();
        }
        

        // 단일/다중 값: [ShowIf("lookupMode", Mode.A)]
        //               [ShowIf("lookupMode", Mode.A, Mode.B)]
        
        public ShowIfAttribute(string conditionFieldName, object value)
        {
            ConditionFieldName = conditionFieldName;
            Condition = ShowIfCondition.Equals;    // 기본은 Equals / In
            RawValues = new[] { value };
        }
        public ShowIfAttribute(string conditionFieldName, params object[] values)
        {
            ConditionFieldName = conditionFieldName;
            Condition = ShowIfCondition.In; 
            RawValues = values ?? Array.Empty<object>();
        }
    }
}