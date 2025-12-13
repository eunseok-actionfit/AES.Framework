using System;


namespace AES.Tools.TBC.Diagnostics
{
    /// <summary>
    /// 유니티 메인 스레드 실행을 보장하기 위한 가드.<br/>
    /// 디버그 환경에서만 유효성 검사를 수행한다.
    /// </summary>
    public static class MainThreadGuard
    {
        // 메인 스레드 ID
        private static readonly int _mainThreadId;

        static MainThreadGuard()
        {
            _mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// 현재 스레드가 유니티 메인 스레드인지 검증한다.<br/>
        /// 아닐 경우 예외를 발생시킨다.
        /// </summary>
        public static void AssertMainThread()
        {
            if (System.Threading.Thread.CurrentThread.ManagedThreadId != _mainThreadId)
                throw new InvalidOperationException("Unity main thread required");
        }
#else
        /// <summary>
        /// 릴리스 빌드에서는 검사하지 않는다.
        /// </summary>
        public static void AssertMainThread() { }
#endif
    }
}