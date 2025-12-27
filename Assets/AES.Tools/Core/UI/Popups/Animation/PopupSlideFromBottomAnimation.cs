using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;

namespace AES.Tools
{
    public sealed class PopupSlideFromBottomAnimation : MonoBehaviour, IPopupAnimation
    {
        [SerializeField] float duration = 0.2f;
        [SerializeField] float offsetY = -600f;

        RectTransform _rt;
        Vector2 _origin;
        CancellationToken _ct;

        void Awake()
        {
            _rt = transform as RectTransform;
            _origin = _rt.anchoredPosition;
            _ct = this.GetCancellationTokenOnDestroy();
        }

        public async UniTask PlayIn()
        {
            _rt.anchoredPosition = _origin + Vector2.up * offsetY;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _rt.anchoredPosition =
                    Vector2.Lerp(_origin + Vector2.up * offsetY, _origin, t / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, _ct);
            }

            _rt.anchoredPosition = _origin;
        }

        public async UniTask PlayOut()
        {
            float t = duration;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime;
                _rt.anchoredPosition =
                    Vector2.Lerp(_origin + Vector2.up * offsetY, _origin, t / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, _ct);
            }
        }
    }
}