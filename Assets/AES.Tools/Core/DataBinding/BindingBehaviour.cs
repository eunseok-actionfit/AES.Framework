// BindingBehaviour.cs
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

        // =====================================================================
        // Debug 필드/헬퍼
        // =====================================================================
#if UNITY_EDITOR
        [SerializeField, HideInInspector] private string _debugContextName;
        [SerializeField, HideInInspector] private string _debugMemberPath;
        [SerializeField, HideInInspector] private string _debugLastValue;
        [SerializeField, HideInInspector] private string _debugLastError;
        [SerializeField, HideInInspector] private int    _debugUpdateCount;

        [SerializeField, HideInInspector] private string _debugProviderObject;
        [SerializeField, HideInInspector] private string _debugProviderType;
        [SerializeField, HideInInspector] private string _debugRuntimeContextType;
        [SerializeField, HideInInspector] private string _debugFullPath;
        [SerializeField, HideInInspector] private int    _debugFrameSubscribed;
        [SerializeField, HideInInspector] private int    _debugFrameFirstUpdate;

        /// <summary>
        /// Provider + memberPath 정보 기록 (ContextBindingBase / ContextListenerBase에서 호출).
        /// </summary>
        protected void Debug_OnSubscribeStart(IBindingContextProvider provider, string memberPath)
        {
            _debugFrameSubscribed = Time.frameCount;
            _debugMemberPath      = memberPath ?? string.Empty;

            if (provider is MonoBehaviour mb)
            {
                _debugProviderObject = mb.gameObject.name;
            }
            else
            {
                _debugProviderObject = provider != null ? provider.ToString() : "NULL";
            }

            _debugProviderType = provider != null ? provider.GetType().Name : "NULL";

            if (provider is MonoContext mc)
            {
                _debugContextName = mc.ContextName;
                _debugRuntimeContextType = mc.RuntimeContext != null
                    ? mc.RuntimeContext.GetType().Name
                    : "NULL";
            }
            else if (provider is MonoBehaviour mb2)
            {
                _debugContextName = mb2.gameObject.name;
                _debugRuntimeContextType = "NULL";
            }
            else
            {
                _debugContextName = "NULL";
                _debugRuntimeContextType = "NULL";
            }
        }

        /// <summary>
        /// RuntimeContext 준비 시점에 타입 기록.
        /// </summary>
        protected void Debug_OnContextReady(IBindingContext context)
        {
            _debugRuntimeContextType = context != null
                ? context.GetType().Name
                : "NULL";
        }

        /// <summary>
        /// 값 업데이트 기록 (Value/Count/FullPath/FirstUpdateFrame).
        /// </summary>
        internal void Debug_OnValueUpdated(object value, string fullPath = null)
        {
            _debugLastValue = value != null ? value.ToString() : "null";
            _debugUpdateCount++;

            if (!string.IsNullOrEmpty(fullPath))
                _debugFullPath = fullPath;

            if (_debugFrameFirstUpdate == 0)
                _debugFrameFirstUpdate = Time.frameCount;
        }

        protected void Debug_ClearRuntimeInfo()
        {
            _debugLastValue         = "";
            _debugLastError         = "";
            _debugUpdateCount       = 0;
            _debugProviderObject    = "";
            _debugProviderType      = "";
            _debugRuntimeContextType= "";
            _debugFullPath          = "";
            _debugFrameSubscribed   = 0;
            _debugFrameFirstUpdate  = 0;
        }
#endif
    }
}
