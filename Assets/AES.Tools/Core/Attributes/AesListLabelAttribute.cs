using UnityEngine;

namespace AES.Tools
{
    // list/array 요소 라벨을 동적으로 표현할 수 있는 범용 Attribute
    // 예: "@environment + \" / \" + platform"
    public class AesListLabelAttribute : PropertyAttribute
    {
        public readonly string Expression;

        public AesListLabelAttribute(string expression)
        {
            Expression = expression;
        }
    }
}