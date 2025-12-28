using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.UI.Popups
{
    public enum DuplicatePolicy
    {
        IgnoreIfOpen,   // 이미 떠있으면 새로 안 띄움(가장 흔함)
        AllowMultiple,  // 여러 개 허용
        BringToFront    // 이미 떠있으면 최상단으로(간단히 재정렬)
    }

    public sealed class PopupHost : MonoBehaviour
    {
        [Header("Registry")]
        [SerializeField] private PopupRegistry registry;

        [Header("UI")]
        [SerializeField] private Transform container;
        [SerializeField] private GameObject modalBlocker;

        [Header("Default Policy")]
        [SerializeField] private DuplicatePolicy duplicatePolicy = DuplicatePolicy.IgnoreIfOpen;

        private readonly List<GameObject> _stack = new();

        public int Count => _stack.Count;
        

        public bool IsOpen<TView>() where TView : PopupViewBase
        {
            var t = typeof(TView);
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i] == null) continue;
                if (_stack[i].GetComponentInChildren<PopupViewBase>(true)?.GetType() == t)
                    return true;
            }
            return false;
        }

        public UniTask<TResult> ShowAsync<TView, TResult>(
            PopupViewModelBase<TResult> vm,
            bool modal = true,
            DuplicatePolicy? policyOverride = null)
            where TView : PopupViewBase
        {
            if (registry == null) throw new Exception("PopupHost: registry가 필요합니다.");
            if (container == null) throw new Exception("PopupHost: container가 필요합니다.");
            if (vm == null) throw new ArgumentNullException(nameof(vm));

            var viewType = typeof(TView);
            var policy = policyOverride ?? duplicatePolicy;

            if (policy != DuplicatePolicy.AllowMultiple)
            {
                var existing = FindInstance(viewType);
                if (existing != null)
                {
                    if (policy == DuplicatePolicy.IgnoreIfOpen)
                        throw new InvalidOperationException(
                            "DuplicatePolicy.IgnoreIfOpen 은 ShowAsync에서 사용할 수 없습니다. TryShowAsync를 사용하세요.");

                    
                    if (policy == DuplicatePolicy.BringToFront)
                        BringToFront(existing);
                }
            }

            return ShowInternal<TView, TResult>(vm, modal, policy);
        }

        // IgnoreIfOpen은 "결과 즉시 반환"이 필요하므로, TryShow 형태를 제공하는 게 안전
        public bool TryShowAsync<TView, TResult>(
            PopupViewModelBase<TResult> vm,
            out UniTask<TResult> task,
            bool modal = true,
            DuplicatePolicy? policyOverride = null)
            where TView : PopupViewBase
        {
            var viewType = typeof(TView);
            var policy = policyOverride ?? duplicatePolicy;

            if (policy != DuplicatePolicy.AllowMultiple)
            {
                var existing = FindInstance(viewType);
                if (existing != null)
                {
                    if (policy == DuplicatePolicy.IgnoreIfOpen)
                    {
                        task = default;
                        return false;
                    }
                    if (policy == DuplicatePolicy.BringToFront)
                    {
                        BringToFront(existing);
                        task = default;
                        return false;
                    }
                }
            }

            task = ShowInternal<TView, TResult>(vm, modal, policy);
            return true;
        }

        private UniTask<TResult> ShowInternal<TView, TResult>(
            PopupViewModelBase<TResult> vm,
            bool modal,
            DuplicatePolicy policy)
            where TView : PopupViewBase
        {
            var prefab = registry.Resolve(typeof(TView));
            if (prefab == null)
                throw new Exception($"PopupHost: '{typeof(TView).Name}' 프리팹을 registry에서 찾지 못했습니다.");

            if (modalBlocker != null)
                modalBlocker.SetActive(modal || _stack.Count > 0);

            var go = Instantiate(prefab, container, false);
            _stack.Add(go);

            // VM 주입(프리팹 내부에 MonoContext가 있으면)
            var monoCtx = go.GetComponentInChildren<MonoContext>(true);
            if (monoCtx != null)
                monoCtx.SetViewModel(vm);

            return AwaitAndClose(vm, go);
        }

        private async UniTask<TResult> AwaitAndClose<TResult>(
            PopupViewModelBase<TResult> vm,
            GameObject go)
        {
            var anim = go.GetComponentInChildren<IPopupAnimation>(true);

            if (anim != null)
                await anim.PlayIn();

            TResult result;
            try
            {
                result = await vm.Result;
            }
            finally
            {
              
                    try { vm.Dispose(); } catch { /* 필요하면 로깅 */ }

                    if (anim != null)
                        await anim.PlayOut();

                    Remove(go);
                    Destroy(go);

                    if (modalBlocker != null)
                        modalBlocker.SetActive(_stack.Count > 0);
            }

            return result;
        }

        private GameObject FindInstance(Type viewType)
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                var go = _stack[i];
                if (go == null) continue;

                var view = go.GetComponentInChildren<PopupViewBase>(true);
                if (view != null && view.GetType() == viewType)
                    return go;
            }
            return null;
        }

        private void BringToFront(GameObject go)
        {
            if (go == null) return;
            go.transform.SetAsLastSibling();
        }

        private void Remove(GameObject go)
        {
            if (go == null) return;
            _stack.Remove(go);
        }
        
        public void CloseTop()
        {
            if (_stack.Count <= 0) return;

            var top = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);

            if (top != null) Destroy(top);

            if (modalBlocker != null)
                modalBlocker.SetActive(_stack.Count > 0);
        }
    }
}
