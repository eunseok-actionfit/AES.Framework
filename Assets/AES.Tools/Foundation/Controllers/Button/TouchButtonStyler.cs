// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
// using AES.Tools;
//
// [RequireComponent(typeof(TouchButton))]
// [RequireComponent(typeof(Image))]
// public class TouchButtonStyler : MonoBehaviour
// {
//     public TouchButtonStyle style;
//
//     public RectTransform visualsRoot; // 비워두면 자기 transform 사용
//
//     private const string BUTTON_NAME = "Button";
//     private const string FRAME_NAME  = "Frame";
//     private const string LABEL_NAME  = "Label";
//
//     private TouchButton _touchButton;
//     private Image _rootImage;
//
//     void Awake()
//     {
//         _touchButton = GetComponent<TouchButton>();
//         _rootImage   = GetComponent<Image>();
//         if (visualsRoot == null)
//             visualsRoot = (RectTransform)transform;
//
//         Apply();
//     }
//
// #if UNITY_EDITOR
//     void OnValidate()
//     {
//         if (!Application.isPlaying)
//         {
//             _touchButton = GetComponent<TouchButton>();
//             _rootImage   = GetComponent<Image>();
//             if (visualsRoot == null)
//                 visualsRoot = (RectTransform)transform;
//             Apply();
//         }
//     }
// #endif
//
//     public void SetStyle(TouchButtonStyle newStyle)
//     {
//         style = newStyle;
//         Apply();
//     }
//
//     public void Apply()
//     {
//         if (style == null || _touchButton == null || _rootImage == null)
//             return;
//
//         var root = (Transform)visualsRoot;
//
//         // ─────────────────────────────
//         // CASE 1 : Frame OFF → 루트 = 버튼
//         // ─────────────────────────────
//         if (!style.useFrame)
//         {
//             DeleteChild(root, BUTTON_NAME);
//             DeleteChild(root, FRAME_NAME);
//
//             _rootImage.enabled       = true;
//             _rootImage.raycastTarget = true;
//             _rootImage.color         = Color.white;
//
//             ApplyImageType(_rootImage, style.normalSprite,
//                 style.buttonType, style.buttonPPUMultiplier);
//
//             _touchButton.targetGraphic      = _rootImage;
//             _touchButton.PressedSprite      = style.pressedSprite;
//             _touchButton.DisabledSprite     = style.disabledSprite;
//             _touchButton.HighlightedSprite  = style.highlightedSprite;
//
//             var labelRT = ApplyText(root);
//             OrderSiblings(root, null, null, labelRT);
//             return;
//         }
//
//         // ─────────────────────────────
//         // CASE 2 : Frame ON & Background OFF
//         //  → 루트 = 버튼, Frame 자식만
//         // ─────────────────────────────
//         if (!style.useBackground)
//         {
//             DeleteChild(root, BUTTON_NAME);
//
//             _rootImage.enabled       = true;
//             _rootImage.raycastTarget = true;
//             _rootImage.color         = Color.white;
//
//             ApplyImageType(_rootImage, style.normalSprite,
//                 style.buttonType, style.buttonPPUMultiplier);
//
//             _touchButton.targetGraphic      = _rootImage;
//             _touchButton.PressedSprite      = style.pressedSprite;
//             _touchButton.DisabledSprite     = style.disabledSprite;
//             _touchButton.HighlightedSprite  = style.highlightedSprite;
//
//             RectTransform frameRT = null;
//             if (style.frameSprite != null)
//             {
//                 frameRT = FindOrCreateImageChild(root, FRAME_NAME);
//                 var frameImage = frameRT.GetComponent<Image>();
//                 ApplyImageType(frameImage, style.frameSprite,
//                     style.frameType, style.framePPUMultiplier);
//                 frameImage.color         = style.frameColor;
//                 frameImage.raycastTarget = false;
//             }
//             else
//             {
//                 DeleteChild(root, FRAME_NAME);
//             }
//
//             var labelRT = ApplyText(root);
//             OrderSiblings(root, null, frameRT, labelRT);
//             return;
//         }
//
//         // ─────────────────────────────
//         // CASE 3 : Frame ON & Background ON
//         //  → 루트 = Background, Button + Frame 자식
//         // ─────────────────────────────
//
//         // Background = 루트
//         _rootImage.enabled       = true;
//         _rootImage.raycastTarget = false;
//         ApplyImageType(_rootImage, style.backgroundSprite,
//             style.backgroundType, style.backgroundPPUMultiplier);
//         _rootImage.color = style.backgroundColor;
//
//         // Button 자식
//         var buttonRT    = FindOrCreateImageChild(root, BUTTON_NAME);
//         var buttonImage = buttonRT.GetComponent<Image>();
//         buttonImage.raycastTarget = true;
//         ApplyImageType(buttonImage, style.normalSprite,
//             style.buttonType, style.buttonPPUMultiplier);
//         buttonImage.color = Color.white;
//
//         _touchButton.targetGraphic      = buttonImage;
//         _touchButton.PressedSprite      = style.pressedSprite;
//         _touchButton.DisabledSprite     = style.disabledSprite;
//         _touchButton.HighlightedSprite  = style.highlightedSprite;
//
//         // Frame 자식
//         RectTransform frameRT2 = null;
//         if (style.frameSprite != null)
//         {
//             frameRT2 = FindOrCreateImageChild(root, FRAME_NAME);
//             var frameImage = frameRT2.GetComponent<Image>();
//             ApplyImageType(frameImage, style.frameSprite,
//                 style.frameType, style.framePPUMultiplier);
//             frameImage.color         = style.frameColor;
//             frameImage.raycastTarget = false;
//         }
//         else
//         {
//             DeleteChild(root, FRAME_NAME);
//         }
//
//         var labelRT3 = ApplyText(root);
//         OrderSiblings(root, buttonRT, frameRT2, labelRT3);
//     }
//
//     // =====================================================================
//     // Text
//     // =====================================================================
//
//     RectTransform ApplyText(Transform root)
//     {
//         if (!style.useText)
//         {
//             DeleteChild(root, LABEL_NAME);
//             return null;
//         }
//
//         var rt = root.Find(LABEL_NAME) as RectTransform;
//         if (rt == null)
//         {
//             var go = new GameObject(LABEL_NAME,
//                 typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
//             rt = go.GetComponent<RectTransform>();
//             rt.SetParent(root, false);
//             rt.anchorMin = Vector2.zero;
//             rt.anchorMax = Vector2.one;
//             rt.offsetMin = Vector2.zero;
//             rt.offsetMax = Vector2.zero;
//
//             var text = go.GetComponent<TextMeshProUGUI>();
//             text.alignment = TextAlignmentOptions.Center;
//             text.text      = "Button";
//         }
//
//         var label = rt.GetComponent<TextMeshProUGUI>();
//         var ts    = style.textStyle;
//
//         if (ts != null)
//         {
//             if (ts.fontAsset != null)
//                 label.font = ts.fontAsset;
//             if (ts.fontMaterial != null)
//                 label.fontSharedMaterial = ts.fontMaterial;
//
//             label.fontSize         = ts.fontSize;
//             label.lineSpacing      = ts.lineSpacing;
//             label.characterSpacing = ts.characterSpacing;
//             label.color            = ts.color;
//             label.richText         = ts.richText;
//             label.raycastTarget    = ts.raycastTarget;
//         }
//
//         return rt;
//     }
//
//     // =====================================================================
//     // Helpers
//     // =====================================================================
//
//     void ApplyImageType(Image img, Sprite sprite, Image.Type type, float ppu)
//     {
//         if (img == null)
//             return;
//
//         img.type   = type;
//         img.sprite = sprite;
//         img.pixelsPerUnitMultiplier = Mathf.Max(ppu, 0.001f);
//     }
//
//     RectTransform FindOrCreateImageChild(Transform root, string name)
//     {
//         var rt = root.Find(name) as RectTransform;
//         if (rt != null)
//             return rt;
//
//         var go = new GameObject(name,
//             typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
//         rt = go.GetComponent<RectTransform>();
//         rt.SetParent(root, false);
//         rt.anchorMin = Vector2.zero;
//         rt.anchorMax = Vector2.one;
//         rt.offsetMin = Vector2.zero;
//         rt.offsetMax = Vector2.zero;
//         return rt;
//     }
//
//     void DeleteChild(Transform root, string name)
//     {
//         var t = root.Find(name);
//         if (t == null)
//             return;
//
// #if UNITY_EDITOR
//         if (!Application.isPlaying)
//             DestroyImmediate(t.gameObject);
//         else
//             Destroy(t.gameObject);
// #else
//         Destroy(t.gameObject);
// #endif
//     }
//
//     void OrderSiblings(Transform root, RectTransform buttonRT, RectTransform frameRT, RectTransform labelRT)
//     {
//         int idx = 0;
//
//         if (buttonRT != null)
//             buttonRT.SetSiblingIndex(idx++);
//
//         if (frameRT != null)
//             frameRT.SetSiblingIndex(idx++);
//
//         if (labelRT != null)
//             labelRT.SetAsLastSibling();
//     }
// }
