using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;


namespace AES.Tools
{
    /// <summary>
    /// 레이어 전면 블로커에 붙여서 "바깥 클릭" 이벤트를 UIManager로 전달.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        public UnityEvent OnClickedEvent;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClickedEvent?.Invoke();
        }
    }
}