using UnityEngine;


namespace AES.Tools
{
    /// <summary>
    /// 순수 StatefulObject(T)를 감싸서 씬에 올리는 래퍼.
    /// - MonoBehaviour + IStateMachineOwner
    /// - HUD/디버거는 이 컴포넌트를 target으로 잡으면 된다.
    /// </summary>
    public abstract class FSMHolder<T> : MonoBehaviour, IStateMachineOwner
        where T : StatefulObject, new()
    {
        [Tooltip("Update/FixedUpdate에서 FSM.Update/FixedUpdate를 자동 호출할지 여부")]
        [SerializeField]
        bool _autoTick = true;

        /// <summary>
        /// 실제 FSM 인스턴스 (순수 StatefulObject).
        /// </summary>
        public T Fsm { get; private set; }

        /// <summary>
        /// IStateMachineOwner 구현: HUD / 그래프가 이걸 통해 StateMachine에 접근.
        /// </summary>
        public StateMachine Machine => Fsm?.Machine;

        protected virtual void Awake()
        {
            // 순수 FSM 생성
            Fsm = new T();

            // 초기 상태/전이 정의 등 사용자 초기화 훅
            OnFsmCreated(Fsm);
        }

        protected virtual void OnDestroy()
        {
            OnFsmDestroyed(Fsm);
            Fsm = null;
        }

        /// <summary>
        /// FSM 생성 직후 한 번 호출되는 초기화 훅.
        /// 여기서 SetInitialState, 파라미터 바인딩 등 수행.
        /// </summary>
        protected abstract void OnFsmCreated(T fsm);

        /// <summary>
        /// FSM 파괴 직전에 호출 (리소스 해제 등 필요 시).
        /// </summary>
        protected virtual void OnFsmDestroyed(T fsm) { }

        protected virtual void Update()
        {
            if (_autoTick)
                Fsm?.Update();
        }

        protected virtual void FixedUpdate()
        {
            if (_autoTick)
                Fsm?.FixedUpdate();
        }
    }
}