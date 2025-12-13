using System;
using System.Collections.Generic;


namespace AES.Tools.StateMachine.Core
{
    /// <summary>
    /// 상태 전이를 평가하고 현재 상태를 관리하는 상태 머신.<br/>
    /// 우선순위 기반 전이, 전역 전이(Any)를 지원한다.
    /// </summary>
    public sealed class StateMachine
    {
        private StateNode currentNode;

        private readonly Dictionary<Type, StateNode> nodes = new();
        private readonly HashSet<Transition> anyTransitions = new();

        // ===========================
        // Observable outputs
        // ===========================
        public ObservableProperty<IState> CurrentState { get; } = new();
        public ObservableProperty<string> LastTransitionName { get; } = new("SetState");
        public IState CurrentStateRaw => CurrentState.Value;

        // ===========================
        // Time (for HasExitTime)
        // ===========================
        private float _now;
        private float _stateEnterTime;

        public float Now => _now;
        public float TimeInState => _now - _stateEnterTime;

        /// <summary>
        /// 현재 시간을 주입하며 업데이트.
        /// </summary>
        public void Update(float now)
        {
            _now = now;
            Update();
        }

        /// <summary>
        /// 현재 시간을 주입하며 FixedUpdate.
        /// (TimeInState를 FixedUpdate 로직에서도 일관되게 쓰고 싶을 때 사용)
        /// </summary>
        public void FixedUpdate(float now)
        {
            _now = now;
            FixedUpdate();
        }

        public void SetState(IState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var previous = currentNode?.State;
            previous?.OnExit();

            currentNode = GetOrAddNode(state);
            currentNode.State?.OnEnter();

            _stateEnterTime = _now; // ★ 진입 시점 기록

            LastTransitionName.Value = "SetState";
            CurrentState.Value = currentNode.State;
        }

        public void Update()
        {
            var t = GetTransition();

            if (t != null)
            {
                ChangeState(t);

                foreach (var node in nodes.Values)
                    ResetActionPredicateFlags(node.Transitions);

                ResetActionPredicateFlags(anyTransitions);
            }

            currentNode?.State?.Update();
        }

        public void FixedUpdate()
            => currentNode?.State?.FixedUpdate();

        public void AddTransition(IState from, IState to, IPredicate condition, int priority = 0, string name = null)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition, priority, name);
        }

        public void AddAnyTransition(IState to, IPredicate condition, int priority = 0, string name = null)
        {
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            anyTransitions.Add(new Transition(GetOrAddNode(to).State, condition, priority, name));
        }

        private Transition GetTransition()
        {
            Transition best = null;
            int prio = int.MinValue;

            foreach (var t in anyTransitions)
                if (t.Evaluate() && t.Priority >= prio)
                    (best, prio) = (t, t.Priority);

            if (currentNode != null)
            {
                foreach (var t in currentNode.Transitions)
                    if (t.Evaluate() && t.Priority >= prio)
                        (best, prio) = (t, t.Priority);
            }

            return best;
        }

        private void ChangeState(Transition t)
        {
            if (t == null) return;

            var next = t.To;
            if (next == null) return;

            if (currentNode != null && ReferenceEquals(next, currentNode.State))
                return;

            var previous = currentNode?.State;
            previous?.OnExit();

            currentNode = GetOrAddNode(next);
            currentNode.State?.OnEnter();

            _stateEnterTime = _now; // ★ 전이 진입 시점 기록

            LastTransitionName.Value = string.IsNullOrEmpty(t.Name) ? "Transition" : t.Name;
            CurrentState.Value = currentNode.State;
        }

        private static void ResetActionPredicateFlags(IEnumerable<Transition> transitions)
        {
            foreach (var t in transitions)
            {
                if (t.Condition is ActionPredicate ap)
                    ap.flag = false;
            }
        }

        private StateNode GetOrAddNode(IState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var type = state.GetType();

            if (!nodes.TryGetValue(type, out var node))
            {
                node = new StateNode(state);
                nodes[type] = node;
            }

            return node;
        }

        private sealed class StateNode
        {
            public IState State { get; }
            public HashSet<Transition> Transitions { get; } = new();

            public StateNode(IState state) => State = state;

            public void AddTransition(IState to, IPredicate pred, int prio, string name)
                => Transitions.Add(new Transition(to, pred, prio, name));
        }

        // ===========================
        // GraphWindow 호환용 디버그 API (유지)
        // ===========================
        public struct StateInfo
        {
            public string Name;
            public IState Instance;
        }

        public struct TransitionInfo
        {
            public string From;
            public string To;
            public string ConditionType;
            public int Priority;
            public string Name;
        }

        public void GetDebugSnapshot(List<StateInfo> statesOut, List<TransitionInfo> transitionsOut)
        {
            statesOut.Clear();
            transitionsOut.Clear();

            foreach (var node in nodes.Values)
            {
                statesOut.Add(new StateInfo
                {
                    Name = node.State.GetType().Name,
                    Instance = node.State
                });

                foreach (var t in node.Transitions)
                {
                    transitionsOut.Add(new TransitionInfo
                    {
                        From = node.State.GetType().Name,
                        To = t.To.GetType().Name,
                        ConditionType = GetConditionLabel(t),
                        Priority = t.Priority,
                        Name = t.Name
                    });
                }
            }

            foreach (var t in anyTransitions)
            {
                transitionsOut.Add(new TransitionInfo
                {
                    From = "Any",
                    To = t.To.GetType().Name,
                    ConditionType = GetConditionLabel(t),
                    Priority = t.Priority,
                    Name = t.Name
                });
            }
        }

        private static string GetConditionLabel(Transition t)
        {
            var typeName = t.ConditionTypeName ?? t.Condition?.GetType().Name ?? "None";

            var backtick = typeName.IndexOf('`');
            if (backtick > 0) typeName = typeName.Substring(0, backtick);

            return typeName switch
            {
                nameof(ObservableBoolPredicate) => "ObservableBoolParam",
                nameof(BoolParameterPredicate) => "BoolParam",
                nameof(TriggerPredicate) => "Trigger",
                nameof(DelegatePredicate) => "Func",
                nameof(AndPredicate) => "AND",
                nameof(OrPredicate) => "OR",
                nameof(NotPredicate) => "NOT",
                nameof(ActionPredicate) => "Action",
                _ => typeName
            };
        }
    }
}
