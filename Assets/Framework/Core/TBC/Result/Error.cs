using System;


namespace Core.Engine.Result
{
    /// <summary>
    /// 에러 정보 구조체 <br/>
    /// </summary>
    /// <remarks>
    /// - 코드, 메시지, 예외 포함 가능 <br/>
    /// - Error.None: 에러 없음 <br/>
    /// </remarks>
    public readonly struct Error
    {
        public readonly string Code;
        public readonly string Message;
        public readonly Exception Exception;

        public bool HasException => Exception != null;

        public Error(string code, string message, Exception ex = null)
        {
            Code = code;
            Message = message;
            Exception = ex;
        }

        public override string ToString() =>
            HasException
                ? $"{Code}: {Message} ({Exception.GetType().Name})"
                : $"{Code}: {Message}";

        public static readonly Error None = new("", "");
    }
}