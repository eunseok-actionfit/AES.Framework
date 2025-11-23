namespace AES.Tools
{
    /// <summary>
    /// ObservableProperty&lt;int&gt; 전용 TextBinding
    /// int → "100", "Lv. 10", "1,000" 등 포맷 가능
    /// </summary>
    public class IntTextBinding : GenericTextBinding<int>
    {
        // int 전용 커스터마이즈가 필요하면 여기서 ConvertValueToString / BuildFinalText override
        // ex) 음수일 때 색상 바꾸기 등, TextBinding 수준에서 할 일 있으면 추가 가능
    }
}


