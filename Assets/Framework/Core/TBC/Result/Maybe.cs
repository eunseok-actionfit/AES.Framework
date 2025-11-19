namespace Core.Engine.Result
{
    /// <summary>
    /// 값이 있을 수도 있고 없을 수도 있는 컨테이너
    /// - Null 대신 명시적 "없음" 표현
    /// </summary>
    public readonly struct Maybe<T>
    {
        public readonly bool HasValue;
        public readonly T Value;

        public Maybe(T value, bool has)
        {
            HasValue = has;
            Value = value;
        }

        public static Maybe<T> Some(T value) => new Maybe<T>(value, true);
        public static Maybe<T> None => new Maybe<T>(default, false);
    }
}