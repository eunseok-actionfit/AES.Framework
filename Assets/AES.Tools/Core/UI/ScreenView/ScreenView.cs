using UnityEngine;

namespace AES.Tools.View
{
    /// <summary>
    /// 화면(Screen) 전용 View.
    /// 에디터에서 배치한 초기 Parent/LocalPosition 을 기억해두고
    /// 필요할 때 ResetLayout 으로 되돌릴 수 있다.
    /// </summary>
    public abstract class ScreenView : UIView
    {
        Vector3   _initialLocalPos;
        Transform _initialParent;

        protected override void Awake()
        {
            base.Awake();

            _initialLocalPos = transform.localPosition;
            _initialParent   = transform.parent;
        }

        /// <summary>
        /// 에디터에서 배치했던 초기 위치/부모로 되돌린다.
        /// (필요한 화면에서만 호출)
        /// </summary>
        public virtual void ResetLayout()
        {
            if (_initialParent != null)
                transform.SetParent(_initialParent, false);

            transform.localPosition = _initialLocalPos;
        }
    }
}