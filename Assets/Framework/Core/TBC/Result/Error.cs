using System;


namespace AES.Tools
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
        public readonly string Context;     // 예: "settings", "progress", "local", "cloud"
        public readonly bool Retriable;     // 재시도 가능한 오류인지 여부

        public bool HasException => Exception != null;

        public Error(
            string code,
            string message,
            string context = "",
            bool retriable = false,
            Exception ex = null)
        {
            Code = code;
            Message = message;
            Context = context;
            Retriable = retriable;
            Exception = ex;
        }

        public override string ToString()
        {
            var exc = HasException ? $" ({Exception.GetType().Name})" : "";
            return $"{Code}: {Message} [{Context}] Retry={Retriable}{exc}";
        }

        public static readonly Error None = new("", "");
    }
}