using TMPro;
using UnityEngine;


namespace AES.Tools
{

    public class StringVirtualizedItem : MonoBehaviour, IVirtualizedItemBinder
    {
        [SerializeField] private TMP_Text label;

        public void Bind(object data, int index)
        {
            if (label == null) return;
            label.text = $"{index}: {data}";
        }
    }
}


