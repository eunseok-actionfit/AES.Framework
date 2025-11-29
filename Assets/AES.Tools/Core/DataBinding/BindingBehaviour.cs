using System;
using UnityEngine;

namespace AES.Tools
{
    public abstract class BindingBehaviour : MonoBehaviour
    {
        private bool _isSubscribed;

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
#if UNITY_EDITOR
            _debugLastError = message;
#endif
        }

        protected void LogBindingWarning(string message)
        {
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
        [SerializeField, HideInInspector] private string _debugContextName;
        [SerializeField, HideInInspector] private string _debugMemberPath;
        [SerializeField, HideInInspector] private string _debugLastValue;
        [SerializeField, HideInInspector] private string _debugLastError;
        [SerializeField, HideInInspector] private int _debugUpdateCount;

        // Provider(Object) 기반 버전
        protected void Debug_SetContextAndPath(UnityEngine.Object ctxObject, string memberPath, string overrideName = null)
        {
            if (ctxObject == null)
            {
                _debugContextName = "NULL";
            }
            else
            {
                if (!string.IsNullOrEmpty(overrideName))
                    _debugContextName = overrideName;
                else
                    _debugContextName = ctxObject.name;
            }

            _debugMemberPath = memberPath ?? "";
        }

        // 이전 DataContextBase 호출부 호환용 (필요하면 남겨둠)
        protected void Debug_SetContextAndPath(MonoContext ctx, string memberPath)
        {
            string name = ctx != null ? ctx.ContextName : "NULL";
            Debug_SetContextAndPath(ctx, memberPath, name);
        }

        protected void Debug_SetLastValue(object value)
        {
            _debugLastValue  = value != null ? value.ToString() : "null";
            _debugUpdateCount++;
        }

        protected void Debug_ClearRuntimeInfo()
        {
            _debugLastValue   = "";
            _debugLastError   = "";
            _debugUpdateCount = 0;
        }
#endif

        #endregion
    }
}
