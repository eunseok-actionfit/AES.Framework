namespace AES.Tools.StateMachine.Core
{
    public interface IState {
        void Update() { }
        void FixedUpdate() { }
        void OnEnter() { }
        void OnExit() { }
    }
}