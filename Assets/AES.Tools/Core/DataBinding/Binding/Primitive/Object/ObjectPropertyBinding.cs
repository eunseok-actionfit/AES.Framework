


// UnityEngine.Object 기반 타입 전용 제네릭 바인딩
namespace AES.Tools
{
    public abstract class ObjectPropertyBinding<T> : PropertyBindingBase<T>
        where T : UnityEngine.Object { }
}