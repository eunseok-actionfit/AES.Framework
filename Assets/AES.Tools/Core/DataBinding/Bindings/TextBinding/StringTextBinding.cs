namespace AES.Tools
{
    /// <summary>
    /// ObservableProperty&lt;string&gt; 전용 TextBinding
    /// </summary>
    public class StringTextBinding : GenericTextBinding<string>
    {
        // 기본 구현이면 충분. 필요하면 ConvertValueToString을 override 해서
        // null 처리, 트리밍 등 커스터마이즈 가능.
    }
}


