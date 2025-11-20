using System;


namespace AES.Tools
{
    /// <summary>
    /// 성공/실패만 표현하는 결과 타입
    /// - 실패 시 Error 포함
    /// </summary>
    public readonly struct Result
    {
        public readonly bool IsSuccess;
        public readonly Error Error;

        private Result(bool ok, Error error)
        {
            IsSuccess = ok;
            Error = error;
        }

        public static Result Ok() => new Result(true, Error.None);
        public static Result Fail(Error error) => new Result(false, error);

        public override string ToString() => IsSuccess ? "Ok" : $"Fail({Error})";
    }

    /// <summary>
    /// 값이 있는 성공/실패 결과 타입
    /// </summary>
    /// <remarks>
    /// - 성공 시 Value + Error.None <br/>
    /// - 실패 시 Error + default(Value) <br/>
    /// - Map/Bind로 체이닝 가능 (함수형 스타일) <br/>
    /// </remarks>
    public readonly struct Result<T>
    {
        public readonly bool IsSuccess;
        public readonly T Value;
        public readonly Error Error;

        public Result(bool ok, T value, Error error)
        {
            IsSuccess = ok;
            Value = value;
            Error = error;
        }

        public static Result<T> Ok(T value) => new Result<T>(true, value, Error.None);
        public static Result<T> Fail(Error error) => new Result<T>(false, default, error);

        public Result<U> Map<U>(Func<T, U> map) =>
            IsSuccess ? Result<U>.Ok(map(Value)) : Result<U>.Fail(Error);

        public Result<U> Bind<U>(Func<T, Result<U>> bind) =>
            IsSuccess ? bind(Value) : Result<U>.Fail(Error);

        public override string ToString() =>
            IsSuccess ? $"Ok({Value})" : $"Fail({Error})";
    }
}