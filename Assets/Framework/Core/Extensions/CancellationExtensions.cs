using System;
using System.Threading;

namespace Extensions
{
    public static class CtsEx
    {
        public static CancellationTokenSource Link(this CancellationToken ct)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(ct);
        }

        
        public static void SafeCancelDispose(this CancellationTokenSource cts)
        {
            if (cts == null) return;
            try {
                if (!cts.IsCancellationRequested) cts.Cancel();
            }
            catch (ObjectDisposedException) { /* ignore */ }
            finally {
                try { cts.Dispose(); } catch { /* ignore */ }
            }
        }
    }
}