using TMPro;
using UnityEngine;


namespace AES.Tools.Controllers.Text
{
    [RequireComponent(typeof(TMP_Text))]
    public class UITextStyler : MonoBehaviour
    {
        public UITextStyle style;
        TMP_Text _text;

        void Awake()
        {
            _text = GetComponent<TMP_Text>();
            Apply();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying)
            {
                _text = GetComponent<TMP_Text>();
                Apply();
            }
        }
#endif

        void Apply()
        {
            if (style == null || _text == null) return;

            if (style.fontAsset != null)
                _text.font = style.fontAsset;
            if (style.fontMaterial != null)
                _text.fontSharedMaterial = style.fontMaterial;

            _text.fontSize         = style.fontSize;
            _text.lineSpacing      = style.lineSpacing;
            _text.characterSpacing = style.characterSpacing;
            _text.color            = style.color;
            _text.richText         = style.richText;
            _text.raycastTarget    = style.raycastTarget;
        }
    }
}