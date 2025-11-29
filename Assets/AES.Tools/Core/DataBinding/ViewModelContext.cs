using System;

namespace AES.Tools
{
    /// <summary>
    /// 하나의 root 객체(ViewModel/데이터)를 감싸는 순수 C# 컨텍스트 베이스.
    /// MemberPath 기반 Get/Set 구현을 제공하고,
    /// 구독(리스너) 관련 부분은 파생 클래스에서 구현한다.
    /// </summary>
    public abstract class ViewModelContext : IBindingContext
    {
        protected readonly object Root;
        protected readonly Type RootType;

        protected ViewModelContext(object root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            RootType = root.GetType();
        }

        protected MemberPath GetMemberPath(string path)
        {
            return MemberPathCache.Get(RootType, path);
        }

        public virtual object GetValue(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Root;

            var mp = GetMemberPath(path);
            return mp.GetValue(Root);
        }

        public virtual void SetValue(string path, object value)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var mp = GetMemberPath(path);
            var tokens = mp.Tokens;
            if (tokens == null || tokens.Count == 0)
                return;

            object current = Root;

            // 마지막 토큰 직전까지 내려감
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                var t = tokens[i];
                current = t switch
                {
                    MemberToken m => GetMember(current, m.Name),
                    IndexToken idx => GetIndex(current, idx.Index),
                    KeyToken key => GetKey(current, key.Key),
                    _ => current
                };

                if (current == null)
                    return;
            }

            var last = tokens[^1];
            if (last is MemberToken lm)
                SetMember(current, lm.Name, value);
            // todo  인덱서/딕셔너리 셋터도 이쪽에서 확장
        }

        public abstract object RegisterListener(string path, Action<object> onValueChanged);
        public abstract void RemoveListener(string path, Action<object> onValueChanged, object token = null);

        #region Helper

        protected static object GetMember(object obj, string name)
        {
            if (obj == null) return null;
            var type = obj.GetType();

            var prop = type.GetProperty(name);
            if (prop != null) return prop.GetValue(obj);

            var field = type.GetField(name);
            if (field != null) return field.GetValue(obj);

            return null;
        }

        protected static void SetMember(object obj, string name, object value)
        {
            if (obj == null) return;

            var type = obj.GetType();

            var prop = type.GetProperty(name);
            if (prop != null)
            {
                var t = prop.PropertyType;
                prop.SetValue(obj, Convert.ChangeType(value, t));
                return;
            }

            var field = type.GetField(name);
            if (field != null)
            {
                var t = field.FieldType;
                field.SetValue(obj, Convert.ChangeType(value, t));
            }
        }

        protected static object GetIndex(object obj, int index)
        {
            if (obj is System.Collections.IList list)
            {
                if (index >= 0 && index < list.Count)
                    return list[index];
            }

            return null;
        }

        protected static object GetKey(object obj, string key)
        {
            if (obj is System.Collections.IDictionary dict)
            {
                if (dict.Contains(key))
                    return dict[key];
            }

            return null;
        }

        #endregion
    }
}
