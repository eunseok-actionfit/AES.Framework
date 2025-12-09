using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AES.Tools;


public sealed class SaveDataInfo
{
    public Type Type;
    public string Id;
    public bool UseSlot;
    public SaveBackend Backend;
}


public static class SaveDataRegistry
{
    public static readonly IReadOnlyList<SaveDataInfo> All;


    static SaveDataRegistry()
    {
        All = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<SaveDataAttribute>() != null)
            .Select(t =>
            {
                var a = t.GetCustomAttribute<SaveDataAttribute>();
                return new SaveDataInfo
                {
                    Type = t,
                    Id = a.Id,
                    UseSlot = a.UseSlot,
                    Backend = a.Backend
                };
            })
            .ToList();
    }
}