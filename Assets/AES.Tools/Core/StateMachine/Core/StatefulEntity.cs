using UnityEngine;


namespace AES.Tools.StateMachine.Core
{
    /// <summary>
    /// Unity <see cref="MonoBehaviour"/> 기반 상태 머신 소유자.<br/>
    /// Update / FixedUpdate 루프를 자동으로 연결한다.
    /// </summary>
    /// <remarks>
    /// <para><b>전형적인 파생 클래스 예</b></para>
    /// <code>
    /// public class Player : StatefulEntity
    /// {
    ///     IdleState idle = new();
    ///     MoveState move = new();
    ///
    ///     TriggerParameter moveTrigger = new();
    ///
    ///     protected override void Awake()
    ///     {
    ///         base.Awake();
    ///
    ///         stateMachine.SetState(idle);
    ///
    ///         At(idle, move, moveTrigger.AsTrigger(), 5, "Idle-&gt;Move");
    ///     }
    ///
    ///     void Update()
    ///     {
    ///         if (Input.GetKeyDown(KeyCode.Space))
    ///             moveTrigger.Fire();
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public abstract class StatefulEntity : MonoBehaviour, IStateMachineOwner
    {
        protected StateMachine stateMachine;
        public StateMachine Machine => stateMachine;

        protected virtual void Awake()
        {
            stateMachine = new StateMachine();
        }

        protected virtual void Update()
            => stateMachine.Update();

        protected virtual void FixedUpdate()
            => stateMachine.FixedUpdate();

        protected void At(IState from, IState to, IPredicate condition, int priority = 0, string name = null)
            => stateMachine.AddTransition(from, to, condition, priority, name);

        protected void Any(IState to, IPredicate condition, int priority = 0, string name = null)
            => stateMachine.AddAnyTransition(to, condition, priority, name);
    }
}
