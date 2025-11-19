using System.Collections.Generic;


namespace Core.Systems.UI.Core.UIRoot
{

    public interface IUIRootProvider
    {
        UIRoot Get(UIRootRole role);  // null 가능
        void Register(UIRoot root);
        void Unregister(UIRoot root);
    }

    /// <summary>
    /// 씬 생명주기에 맞춰 UIRoot 등록/해제만 담당하는 가벼운 Provider.
    /// (비-Mono 싱글톤이므로 DI로 한 번 주입해 쓰면 끝)
    /// </summary>
    public sealed class UIRootProvider : IUIRootProvider
    {
        // 단순 리스트(중복 제거)
        private readonly List<UIRoot> _roots = new();

        public void Register(UIRoot root)
        {
            if (root != null && !_roots.Contains(root)) _roots.Add(root);
        }

        public void Unregister(UIRoot root)
        {
            _roots.Remove(root);
        }

        public UIRoot Get(UIRootRole role)
        {
            for (int i = 0; i < _roots.Count; i++)
            {
                var r = _roots[i];
                if (r != null && r.Role == role)
                    return r;
            }
            return null;
        }
    }
}