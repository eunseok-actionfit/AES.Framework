using AES.Tools.StateMachine.Core;
using UnityEngine;


namespace AES.Tools.StateMachine {
    public sealed class PlayerFSMExample : MonoBehaviour, IStateMachineOwner
    {
        Core.StateMachine sm;
        public Core.StateMachine Machine => sm;

        BoolParameter isGrounded;
        TriggerParameter moveTrigger;
        BoolParameter isDead;

        void Awake()
        {
            // parameters
            isGrounded  = new BoolParameter(true);
            moveTrigger = new TriggerParameter();
            isDead      = new BoolParameter(false);

            // states
            var idle = new IdleState();
            var move = new MoveState();
            var dead = new DeadState();

            // machine
            sm = new Core.StateMachine();
            sm.SetState(idle);
        

            // graph DSL
            var g = new StateGraphBuilder(sm);

            g.From(idle)
                .To(move)
                .When(isGrounded.IsTrue().And(moveTrigger.AsTrigger()))
                .Priority(10)
                .Named("Idle->Move")
                .Add();

            g.From(move)
                .To(idle)
                .When(isGrounded.IsFalse())
                .Named("Move->Idle")
                .Add();

            g.FromAny()
                .To(dead)
                .When(isDead.IsTrue())
                .Priority(100)
                .Named("Any->Dead")
                .Add();
        }

        void Update()
        {
            sm.Update();

            // 테스트 입력
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
                moveTrigger.Fire();

            if (UnityEngine.Input.GetKeyDown(KeyCode.K))
                isDead.Value = true;
        }
    
    }


    public sealed class IdleState : IState
    {
        public void OnEnter()  => Debug.Log("Enter Idle");
        public void OnExit()   => Debug.Log("Exit Idle");
        public void Update()   { }
    }

    public sealed class MoveState : IState
    {
        public void OnEnter()  => Debug.Log("Enter Move");
        public void OnExit()   => Debug.Log("Exit Move");
        public void Update()   { }
    }

    public sealed class DeadState : IState
    {
        public void OnEnter() => Debug.Log("Enter Dead");
    }
}