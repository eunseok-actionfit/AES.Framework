using UnityEngine;


namespace Core.Systems.Input.Core.Utils
{
    namespace Systems.Input.Utils
    {
        public static class DpiScaler
        {
            public static float PxToDevice(float px, float referenceDpi)
            {
                var dpi = Screen.dpi <= 0 ? referenceDpi : Screen.dpi;
                return px * (dpi / referenceDpi);
            }
        }
    }
}


