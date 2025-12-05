using AES.Tools;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "UI/Styles/Touch Button Style")]
public class TouchButtonStyle : ScriptableObject
{
    // ─────────────────────────────
    // Button (상태별 스프라이트)
    // ─────────────────────────────
    [Header("Button Graphic")]
    public Sprite normalSprite;
    public Sprite pressedSprite;
    public Sprite disabledSprite;
    public Sprite highlightedSprite;

    public Image.Type buttonType = Image.Type.Sliced;

    [ShowIf("buttonType", Image.Type.Sliced, Image.Type.Tiled)]
    public float buttonPPUMultiplier = 1f;

    // ─────────────────────────────
    // Text
    // ─────────────────────────────
    [Header("Text")]
    public bool useText = true;
    public UITextStyle textStyle;

    // ─────────────────────────────
    // Frame (테두리 / 아웃라인)
    // ─────────────────────────────
    [Header("Frame")]
    public bool useFrame;

    [ShowIf("useFrame")]
    public Sprite frameSprite;

    [ShowIf("useFrame")]
    public Color frameColor = Color.white;

    [ShowIf("useFrame")]
    public Image.Type frameType = Image.Type.Sliced;

    [ShowIf("useFrame")]
    [ShowIf("frameType", Image.Type.Sliced, Image.Type.Tiled)]
    public float framePPUMultiplier = 1f;

    // ─────────────────────────────
    // Background (프레임 쓸 때, 루트 Image)
    // ─────────────────────────────
    [ShowIf("useFrame")]
    [Header("Background (when framed)")]
    public bool useBackground = true;

    [ShowIf("useFrame")]
    [ShowIf("useBackground")]
    public Sprite backgroundSprite;

    [ShowIf("useFrame")]
    [ShowIf("useBackground")]
    public Color backgroundColor = Color.white;

    [ShowIf("useFrame")]
    [ShowIf("useBackground")]
    public Image.Type backgroundType = Image.Type.Sliced;

    [ShowIf("useFrame")]
    [ShowIf("useBackground")]
    [ShowIf("backgroundType", Image.Type.Sliced, Image.Type.Tiled)]
    public float backgroundPPUMultiplier = 1f;
}