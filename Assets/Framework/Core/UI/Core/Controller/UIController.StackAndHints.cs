// Assets/Framework/Systems/UI/Core/UIManager.StackAndHints.cs
using System.Collections.Generic;
using AES.Tools.Core.Layer;
using AES.Tools.Core.Policies;
using AES.Tools.Core.View;
using UnityEngine;


namespace AES.Tools.Core.Controller
{
    public sealed partial class UIController
    {
        // ─────────────────────────────────────────────────────────────
        // RectTransform 원복용 스냅샷
        private struct RectSnap
        {
            public Vector2 aMin, aMax, oMin, oMax;
        }

        // 풀 반납/재사용 대비 원복용 스냅샷 저장소
        private readonly Dictionary<RectTransform, RectSnap> _rtSnapshot = new();

        // ─────────────────────────────────────────────────────────────
        // Stack 관리 (Layer별 UI 정렬)
        private void PushStack(Transform parent, UIView view)
        {
            if (parent == null || view == null) return;

            if (!_stackByParent.TryGetValue(parent, out var list))
                _stackByParent[parent] = list = new List<UIView>(8);

            var layer = AsLayer(parent);

            // ZPriority 정렬 또는 단순 뒤로 붙이기
            if (layer != null && layer.Policy.SortingPolicy == LayerSortingPolicy.ByZPriority) {
                list.Add(view);
                list.Sort((a, b) => a.ZPriority.CompareTo(b.ZPriority));
                for (int i = 0; i < list.Count; i++)
                    list[i].transform.SetSiblingIndex(i);
            }
            else {
                list.Add(view);
                view.transform.SetAsLastSibling();
            }

            // ModalStack: 최상단만 상호작용 허용
            if (layer != null && layer.Policy.ModalStack) {
                for (int i = 0; i < list.Count; i++) {
                    var top = (i == list.Count - 1);
                    var cg = list[i].CanvasGroup ?? list[i].GetComponent<CanvasGroup>();
                    if (cg != null) cg.blocksRaycasts = top;
                }
            }

            if (layer != null && layer.Policy.BlocksInput) {
                layer.SetInputBlocker(on: list.Count > 0 && layer.Policy.BlocksInput);
                var top = list[^1]; // 최상단 뷰
                layer.PlaceBlockerBelowTop(top.transform); // 블로커를 'top 바로 아래'로
            }
        }

        private void PopStack(Transform parent, UIView view)
        {
            if (parent == null || view == null) return;
            if (!_stackByParent.TryGetValue(parent, out var list)) return;

            list.Remove(view);
            var layer = AsLayer(parent);

            // ModalStack: 최상단만 상호작용 허용 재계산
            if (layer != null && layer.Policy.ModalStack) {
                for (int i = 0; i < list.Count; i++) {
                    var top = (i == list.Count - 1);
                    var cg = list[i].CanvasGroup ?? list[i].GetComponent<CanvasGroup>();
                    if (cg != null) cg.blocksRaycasts = top;
                }
            }

            // InputBlocker 동기화
            if (layer != null && layer.Policy.BlocksInput) {
                if (list.Count == 0) { layer.SetInputBlocker(on: false); }
                else {
                    var top = list[^1];
                    layer.PlaceBlockerBelowTop(top.transform); // 새 top 아래로 재배치
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // UIViewHints 연동
        private static UIViewHints GetHints(UIView v)
            => v ? v.GetComponent<UIViewHints>() : null;

        /// <summary> SafeArea 적용 여부 결정 (Entry > Hints > Layer 기본) </summary>
        private bool GetEffectiveUseSafe(UILayer layer, UIViewHints policy)
        {
            if (policy != null && policy.useSafeArea.enabled)
                return policy.useSafeArea.value;
            return layer == null || layer.Policy.UseSafeArea;
        }

        /// <summary> InputBlocker Override 결정 (Entry > Hints > null=레이어 기본) </summary>
        private bool? GetEffectiveInputBlockerOverride(UIViewHints policy)
        {
            if (policy != null && policy.inputBlocker.enabled)
                return policy.inputBlocker.value;
            return null;
        }

        /// <summary> CloseOn 결정 (Entry > Hints > None) </summary>
        private UICloseOn GetEffectiveCloseOn(UIViewHints policy)
        {
            if (policy != null && policy.closeOn.enabled)
                return policy.closeOn.value;
            return UICloseOn.None;
        }

        // ─────────────────────────────────────────────────────────────
        // 뷰 단 SafeArea/여백 오버라이드 적용 & 원복

        /// <summary>
        /// UIViewHints 기준으로 앵커/오프셋을 조정한다.
        /// (SafeArea 미적용 + 전체 확장, 추가 여백 픽셀 적용)
        /// </summary>
        private void ApplyViewSafeAreaOverrides(UIView view, UIViewHints policy)
        {
            if (!view) return;
            var rt = view.transform as RectTransform;
            if (!rt || policy == null) return;

            // 최초 1회 원본 스냅샷 저장
            if (!_rtSnapshot.ContainsKey(rt)) {
                _rtSnapshot[rt] = new RectSnap
                {
                    aMin = rt.anchorMin, aMax = rt.anchorMax,
                    oMin = rt.offsetMin, oMax = rt.offsetMax
                };
            }

            // SafeArea 미적용 + 전체 화면 스트레치
            bool wantStretch =
                policy.useSafeArea is { enabled: true, value: false } &&
                policy.stretchFullWhenNoSafe is { enabled: true, value: true };

            if (wantStretch) {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // 추가 여백(픽셀) 적용
            int l = (policy.extraLeft.enabled ? Mathf.Max(0, policy.extraLeft.value) : 0);
            int r = (policy.extraRight.enabled ? Mathf.Max(0, policy.extraRight.value) : 0);
            int t = (policy.extraTop.enabled ? Mathf.Max(0, policy.extraTop.value) : 0);
            int b = (policy.extraBottom.enabled ? Mathf.Max(0, policy.extraBottom.value) : 0);

            if (l != 0 || r != 0 || t != 0 || b != 0) {
                var om = rt.offsetMin;
                var ox = rt.offsetMax;
                om.x += l;
                om.y += b; // 좌/하 +
                ox.x -= r;
                ox.y -= t; // 우/상 -
                rt.offsetMin = om;
                rt.offsetMax = ox;
            }
        }

        /// <summary>
        /// 풀 반납/재표시 대비 RectTransform을 원상 복구.
        /// </summary>
        private void RestoreViewRect(UIView view)
        {
            if (!view) return;
            var rt = view.transform as RectTransform;
            if (!rt) return;

            if (_rtSnapshot.TryGetValue(rt, out var s)) {
                rt.anchorMin = s.aMin;
                rt.anchorMax = s.aMax;
                rt.offsetMin = s.oMin;
                rt.offsetMax = s.oMax;
                // 스냅샷은 유지(재표시 시 동일 기준 사용)
            }
        }
        
    }
}