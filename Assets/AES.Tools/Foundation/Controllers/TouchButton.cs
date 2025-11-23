using UnityEngine;
using UnityEngine.EventSystems;


namespace AES.Tools
{
    [RequireComponent(typeof(Rect))]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("AES/Tools/Controls/Touch Button")]
    public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IPointerEnterHandler, ISubmitHandler
    {

        public void OnPointerDown(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            throw new System.NotImplementedException();
        }
        public void OnSubmit(BaseEventData eventData)
        {
            throw new System.NotImplementedException();
        }
    }
    
}


