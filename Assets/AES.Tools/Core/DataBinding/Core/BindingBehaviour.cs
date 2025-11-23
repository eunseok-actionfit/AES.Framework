using System;
using UnityEngine;


namespace AES.Tools
{
    public abstract class BindingBehaviour : MonoBehaviour
    {
        bool _isSubscribed;

        protected virtual void OnEnable()
        {
            if (_isSubscribed)
                return;

            _isSubscribed = true;

            try { Subscribe(); }
            catch (Exception ex) { LogBindingException("Subscribe() 중 예외 발생", ex); }
        }

        protected virtual void OnDisable()
        {
            if (!_isSubscribed)
                return;

            _isSubscribed = false;

            try { Unsubscribe(); }
            catch (Exception ex) { LogBindingException("Unsubscribe() 중 예외 발생", ex); }
        }

        protected abstract void Subscribe();
        protected abstract void Unsubscribe();

        // 공통 로그 헬퍼들
        protected void LogBindingError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}", this);
#if UNITY_EDITOR
            _debugLastError = message;
#endif
        }

        protected void LogBindingWarning(string message)
        {
            Debug.LogWarning($"[{GetType().Name}] {message}", this);
#if UNITY_EDITOR
            _debugLastError = "WARN: " + message;
#endif
        }

        protected void LogBindingException(string message, Exception ex)
        {
            Debug.LogException(new Exception($"[{GetType().Name}] {message}", ex), this);
#if UNITY_EDITOR
            _debugLastError = $"{message}\n{ex}";
#endif
        }

        #region Debug

#if UNITY_EDITOR
        [SerializeField, HideInInspector] string _debugContextName;
        [SerializeField, HideInInspector] string _debugMemberPath;
        [SerializeField, HideInInspector] string _debugLastValue;
        [SerializeField, HideInInspector] string _debugLastError;
        [SerializeField, HideInInspector] int _debugUpdateCount;

        // 디버그 정보 업데이트용 protected 헬퍼들
        protected void Debug_SetContextAndPath(DataContextBase ctx, string memberPath)
        {
            _debugContextName = ctx != null ? ctx.ContextName : "NULL";
            _debugMemberPath = memberPath ?? "";
        }

        protected void Debug_SetLastValue(object value)
        {
            _debugLastValue = value != null ? value.ToString() : "null";
            _debugUpdateCount++;
        }

        protected void Debug_ClearRuntimeInfo()
        {
            _debugLastValue = "";
            _debugLastError = "";
            _debugUpdateCount = 0;
        }
#endif

        #endregion
    }
}