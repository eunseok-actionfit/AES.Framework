using System;
using UnityEngine;
using UnityEngine.UI;


namespace AES.Tools
{
    public class Pressable_Button : MonoBehaviour, IPressable
    {
        public event Action Clicked;
        [SerializeField] private Button button;

        private void Reset() => button = GetComponent<Button>();

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void SetInteractable(bool interactable) => button.interactable = interactable;
    }
}