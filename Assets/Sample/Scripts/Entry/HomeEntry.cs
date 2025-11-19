using System;
using UnityEngine;


namespace Sample.Scripts
{
    public class HomeEntry : MonoBehaviour
    {
        private void Start()
        {
            UI.ShowAsync(GlobalUIId.GlobalHUD);
            UI.ShowAsync(GlobalUIId.HomeHUD);
        }
    }
}


