using System;
using UnityEngine;


namespace Sample.Scripts
{
    public class GameEntry : MonoBehaviour
    {
        private void Start()
        {
            UI.ShowAsync(GlobalUIId.HomeHUD);
        }
    }
}


