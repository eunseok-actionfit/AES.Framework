using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AES.Tools
{
#if UNITY_EDITOR
    /// <summary>
    /// 풀 누수 진단 유틸리티.<br/>
    /// 대여 중 컬렉션을 검사해 로그로 보고한다.
    /// </summary>
    public static class PoolLeakDetector
    {
        /// <summary>
        /// 사용 중 항목을 스캔해 누수 리포트를 출력한다.
        /// </summary>
        /// <typeparam name="T">대상 객체 타입</typeparam>
        /// <param name="poolName">풀 표시 이름</param>
        /// <param name="inUse">현재 대여 중 컬렉션</param>
        public static void LeakReport<T>(string poolName, ICollection<T> inUse)
        {
            if (inUse == null)
            {
                Debug.LogWarning($"[{poolName}] LeakReport called with null InUse collection.");
                return;
            }

            if (inUse.Count == 0)
            {
                Debug.Log($"[{poolName}] No leaks detected.");
                return;
            }

            Debug.LogWarning($"[{poolName}] Leaked {inUse.Count} objects:");
            foreach (var obj in inUse)
            {
                if (obj is Object uObj)
                    Debug.LogWarning($" - {uObj.name}", uObj);
                else
                    Debug.LogWarning($" - {obj}");
            }
        }
    }
#endif
}