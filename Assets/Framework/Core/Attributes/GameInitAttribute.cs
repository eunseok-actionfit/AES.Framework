using System;


namespace AES.Tools
{
    [AttributeUsage(AttributeTargets.Method)]
    public class GameInitAttribute : Attribute
    {
        public int Order { get; }
        public GameInitAttribute(int order = 0) => Order = order;
    }
    
    // var methods = FindAllInitMethodsSortedByOrder();
    // foreach (var m in methods)
    //     await (UniTask)m.Invoke(target, null);
}


