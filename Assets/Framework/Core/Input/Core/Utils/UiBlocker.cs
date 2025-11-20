using UnityEngine.EventSystems;


namespace AES.Tools.Core.Utils
{

    namespace Systems.Input.Utils
    {
        public static class UiBlocker
        {
            public static bool IsPointerOverUI(int pointerId)
            {
                if (EventSystem.current == null) return false;
#if UNITY_EDITOR || UNITY_STANDALONE
                return EventSystem.current.IsPointerOverGameObject();
#else
            return EventSystem.current.IsPointerOverGameObject(pointerId);
#endif
            }
        }
    }
}


