using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Threading;

namespace AES.Tools
{
    public sealed class PopupScaleAnimation : MonoBehaviour, IPopupAnimation
    {
        [SerializeField] float duration = 0.12f;
        [SerializeField] Vector3 from = new(0.9f, 0.9f, 1f);

        RectTransform _rt;
        CancellationToken _ct;

        void Awake()
        {
            _rt = transform as RectTransform;
            _ct = this.GetCancellationTokenOnDestroy();
        }

        public async UniTask PlayIn()
        {
            _rt.localScale = from;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _rt.localScale = Vector3.Lerp(from, Vector3.one, t / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, _ct);
            }

            _rt.localScale = Vector3.one;
        }

        public async UniTask PlayOut()
        {
            float t = duration;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime;
                _rt.localScale = Vector3.Lerp(from, Vector3.one, t / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, _ct);
            }
        }
    }
}