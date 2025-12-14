using UnityEngine;


public sealed class AesListLabelAttribute : PropertyAttribute
{
    public readonly string[] Members;
    public readonly string Format;

    public AesListLabelAttribute(string format, params string[] members)
    {
        Format = format;
        Members = members;
    }

    public AesListLabelAttribute(params string[] members)
    {
        Members = members;
        Format = null;
    }
}