using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PopupFadeAnimation : MonoBehaviour, IPopupAnimation
    {
        [SerializeField] float duration = 0.15f;

        CanvasGroup _cg;

        void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
        }

        public async UniTask PlayIn()
        {
            _cg.alpha = 0f;
            _cg.blocksRaycasts = false;

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _cg.alpha = Mathf.Clamp01(t / duration);
                await UniTask.Yield();
            }

            _cg.alpha = 1f;
            _cg.blocksRaycasts = true;
        }

        public async UniTask PlayOut()
        {
            _cg.blocksRaycasts = false;

            float t = duration;
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime;
                _cg.alpha = Mathf.Clamp01(t / duration);
                await UniTask.Yield();
            }

            _cg.alpha = 0f;
        }
    }
}