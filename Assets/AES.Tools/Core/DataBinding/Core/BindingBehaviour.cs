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

            try
            {
                Subscribe();
            }
            catch (Exception ex)
            {
                LogBindingException("Subscribe() 중 예외 발생", ex);
            }
        }

        protected virtual void OnDisable()
        {
            if (!_isSubscribed)
                return;

            _isSubscribed = false;

            try
            {
                Unsubscribe();
            }
            catch (Exception ex)
            {
                LogBindingException("Unsubscribe() 중 예외 발생", ex);
            }
        }

        protected abstract void Subscribe();
        protected abstract void Unsubscribe();

        // 공통 로그 헬퍼들
        protected void LogBindingError(string message)
        {
            Debug.LogError($"[{GetType().Name}] {message}", this);
        }

        protected void LogBindingWarning(string message)
        {
            Debug.LogWarning($"[{GetType().Name}] {message}", this);
        }

        protected void LogBindingException(string message, Exception ex)
        {
            Debug.LogException(new Exception($"[{GetType().Name}] {message}", ex), this);
        }
    }
}