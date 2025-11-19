using UnityEngine;
using UnityEngine.EventSystems;


namespace Core.Systems.UI.Core
{
    /// <summary>
    /// 레이어 전면 블로커에 붙여서 "바깥 클릭" 이벤트를 UIManager로 전달.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIClickCatcher : MonoBehaviour, IPointerClickHandler
    {
        public System.Action OnClicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClicked?.Invoke();
        }
    }
}