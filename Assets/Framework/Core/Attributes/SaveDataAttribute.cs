using System;


[AttributeUsage(AttributeTargets.Class)]
public sealed class SaveDataAttribute : Attribute
{
    public string Id { get; }
    public bool UseSlot { get; set; } = true;
    public SaveBackend Backend { get; set; } = SaveBackend.LocalOnly;


    public SaveDataAttribute(string id)
    {
        Id = id;
    }
}