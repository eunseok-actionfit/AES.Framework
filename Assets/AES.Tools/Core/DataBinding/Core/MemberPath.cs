using System;
using System.Collections;
using System.Collections.Generic;

namespace AES.Tools
{
    public class MemberPath
    {
        public readonly List<PathToken> Tokens;

        public MemberPath(List<PathToken> tokens)
        {
            Tokens = tokens;
        }

        public object GetValue(object root)
        {
            object current = root;

            foreach (var token in Tokens)
            {
                if (current == null)
                    return null;

                switch (token)
                {
                    case MemberToken m:
                        current = GetMember(current, m.Name);
                        break;

                    case IndexToken idx:
                        current = GetIndex(current, idx.Index);
                        break;

                    case KeyToken key:
                        current = GetKey(current, key.Key);
                        break;

                    default:
                        throw new NotSupportedException($"Unknown token {token.GetType()}");
                }
            }

            return current;
        }

        private object GetMember(object obj, string name)
        {
            var type = obj.GetType();

            var prop = type.GetProperty(name);
            if (prop != null) return prop.GetValue(obj);

            var field = type.GetField(name);
            if (field != null) return field.GetValue(obj);

            return null;
        }

        private object GetIndex(object obj, int index)
        {
            switch (obj)
            {
                case IList list:
                    return index >= 0 && index < list.Count ? list[index] : null;

                default:
                    return null;
            }
        }

        private object GetKey(object obj, string key)
        {
            switch (obj)
            {
                case IDictionary dict:
                    return dict.Contains(key) ? dict[key] : null;

                default:
                    return null;
            }
        }
    }

    public static class MemberPathCache
    {
        private static readonly Dictionary<(Type, string), MemberPath> _cache = new();

        public static MemberPath Get(Type rootType, string path)
        {
            var key = (rootType, path);
            if (_cache.TryGetValue(key, out var mp))
                return mp;

            var tokens = MemberPathParser.Parse(path);
            mp = new MemberPath(tokens);
            _cache[key] = mp;
            return mp;
        }
    }
}