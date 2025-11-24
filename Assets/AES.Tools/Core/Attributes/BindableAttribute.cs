using System;

namespace AES.Tools
{
    /// <summary>
    /// 이 어트리뷰트가 붙은 필드/프로퍼티는
    /// ContextBindingBaseEditor의 Path 드롭다운에 표시된다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class BindableAttribute : Attribute
    {

        public BindableAttribute()
        {
        }
    }
}