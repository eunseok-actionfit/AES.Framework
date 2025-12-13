using System;
using AES.Tools.Controllers.Core;
using UnityEngine;


namespace AES.Tools
{
    public class Pressable_TouchButton : MonoBehaviour, IPressable
    {
        public event Action Clicked;
        [SerializeField] private TouchButton button;

        private void Reset() => button = GetComponent<TouchButton>();

        private void Awake()
        {
            if (button == null) button = GetComponent<TouchButton>();
            button.ButtonTapped.AddListener(() => Clicked?.Invoke());
        }

        public void SetInteractable(bool interactable) => button.interactable = interactable;
    }
}