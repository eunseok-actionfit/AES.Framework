namespace AES.Tools
{
    public abstract class PathToken { }

    public sealed class MemberToken : PathToken
    {
        public string Name;
    }

    public sealed class IndexToken : PathToken
    {
        public int Index;
    }

    public sealed class KeyToken : PathToken
    {
        public string Key;
    }
}