namespace AES.Tools
{
    public sealed class StateGraphBuilder
    {
        readonly StateMachine _machine;

        public StateGraphBuilder(StateMachine machine)
        {
            _machine = machine;
        }

        public StateGraphBuilder From<T>(IState from, IState to, T condition, int priority = 0, string name = null)
        {
            _machine.AddTransition(from, to, condition, priority, name);
            return this;
        }

        public StateGraphBuilder Any<T>(IState to, T condition, int priority = 0, string name = null)
        {
            _machine.AddAnyTransition(to, condition, priority, name);
            return this;
        }
    }

    public static class StateGraphBuilderExtensions
    {
        public static StateGraphBuilder BuildGraph(this StateMachine machine)
            => new StateGraphBuilder(machine);
    }
}