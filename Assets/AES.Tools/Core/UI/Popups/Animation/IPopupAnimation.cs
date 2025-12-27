using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    public interface IPopupAnimation
    {
        UniTask PlayIn();
        UniTask PlayOut();
    }
}