using System;
using System.Collections.Generic;
using System.Reflection;


namespace AES.Tools
{
    public class MemberPath
    {
        private readonly MemberInfo[] _members;
        
        public MemberPath(MemberInfo[] members)
        {
            _members = members;
        }

        public object GetValue(object root)
        {
            var current = root;

            foreach (var m in _members)
            {
                if(current == null) return null;
                
                if(m is PropertyInfo pi)
                    current = pi.GetValue(current);
                else if(m is FieldInfo fi)
                    current = fi.GetValue(current);
            }
            return current;
        }

        public void SetValue(object root, object value)
        {
            if (_members.Length == 0) return;
            
            object current = root;
            for (int i = 0; i < _members.Length - 1; i++)
            {
                var m = _members[i];
                if (m is PropertyInfo pi)
                    current = pi.GetValue(current);
                else if (m is FieldInfo fi)
                    current = fi.GetValue(current);
                
                if (current == null) return;
            }
            
            var last = _members[^1];
            if(last is PropertyInfo lastPi)
                lastPi.SetValue(current, value);
            else if(last is FieldInfo lastFi)
                lastFi.SetValue(current, value);
        }
    }

    public static class MemberPathCache
    {
        private static readonly Dictionary<(Type, string), MemberPath> Cache = new();

        public static MemberPath Get(Type rootType, string path)
        {
            var key = (rootType, path);
            if(Cache.TryGetValue(key, out var existing)) 
                return existing;
            
            var parts = path.Split('.');
            var members = new MemberInfo[parts.Length];
            var currentType = rootType;

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var mi = (MemberInfo)currentType.GetProperty(part,
                    BindingFlags.Instance | BindingFlags.Public) 
                    ?? currentType.GetField(part,
                        BindingFlags.Instance | BindingFlags.Public);

                if (mi == null)
                    throw new InvalidOperationException($"'{currentType.Name}'에 '{part}' 멤버가 없습니다.");
                
                members[i] = mi;
                
                if(mi is PropertyInfo pi)
                    currentType = pi.PropertyType;
                else if(mi is FieldInfo fi)
                    currentType = fi.FieldType;
            }
            
            var pathObj = new MemberPath(members);
            Cache[key] = pathObj;
            return pathObj;
            
        }
    }
}