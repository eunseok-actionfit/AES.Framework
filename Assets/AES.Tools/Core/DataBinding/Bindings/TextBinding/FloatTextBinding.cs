namespace AES.Tools
{
    /// <summary>
    /// ObservableProperty&lt;float&gt; 전용 TextBinding
    /// float → "0.0", "100.0 HP", "12.34%" 등 포맷 가능
    /// </summary>
    public class FloatTextBinding : GenericTextBinding<float>
    {
        // 필요하면 소수점 자리 고정, 단위 추가 등 커스터마이즈 가능
        // 기본은 format / useFormat / culture 로 처리
    }
}


