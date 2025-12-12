using System;

namespace AES.Tools
{
    public sealed class Transition : ITransition
    {
        public IState To { get; }
        public IPredicate Condition { get; }

        public int Priority { get; }
        public string Name { get; }

        internal string ConditionTypeName { get; }

        public Transition(
            IState to,
            IPredicate condition,
            int priority = 0,
            string name = null,
            string conditionTypeName = null)
        {
            To = to ?? throw new ArgumentNullException(nameof(to));
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            Priority = priority;
            Name = name;
            ConditionTypeName = conditionTypeName ?? condition.GetType().Name;
        }

        public bool Evaluate() => Condition.Evaluate();
    }
}