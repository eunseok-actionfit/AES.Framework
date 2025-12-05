// Scripts/Text/UITextStyle.cs
using TMPro;
using UnityEngine;


[CreateAssetMenu(menuName = "UI/Styles/Text Style")]
public class UITextStyle : ScriptableObject
{
    public TMP_FontAsset fontAsset;
    public Material fontMaterial;
    
    public int fontSize = 32;
    public float lineSpacing = 0f;
    public float characterSpacing = 0f;
    
    public Color color = Color.white;
    public bool richText = true;
    public bool raycastTarget = false;
}