using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace AES.Tools
{
    public class StateMachine
    {
        StateNode currentNode;
        readonly Dictionary<Type, StateNode> nodes = new();
        readonly HashSet<Transition> anyTransitions = new();

        public IState CurrentState => currentNode?.State;

        /// <summary>
        /// 상태 변경 이벤트 (이전 상태, 다음 상태, 사용된 전이).
        /// </summary>
        public event Action<IState, IState, Transition> OnStateChanged;

        /// <summary>
        /// 상태 진입 이벤트.
        /// </summary>
        public event Action<IState> OnStateEntered;

        /// <summary>
        /// 상태 종료 이벤트.
        /// </summary>
        public event Action<IState> OnStateExited;

        /// <summary>
        /// 디버그 로그 콜백.
        /// 예: sm.Logger = msg => Debug.Log(msg);
        /// </summary>
        public Action<string> Logger { get; set; }

        public void Update()
        {
            var transition = GetTransition();

            if (transition != null)
            {
                ChangeState(transition);
                foreach (var node in nodes.Values)
                    ResetActionPredicateFlags(node.Transitions);

                ResetActionPredicateFlags(anyTransitions);
            }

            currentNode?.State?.Update();
        }

        static void ResetActionPredicateFlags(IEnumerable<Transition> transitions)
        {
            foreach (var transition in transitions.OfType<Transition<ActionPredicate>>())
                transition.condition.flag = false;
        }

        public void FixedUpdate()
        {
            currentNode?.State?.FixedUpdate();
        }

        public void SetState(IState state)
        {
            var previous = currentNode?.State;
            currentNode  = GetOrAddNode(state);
            currentNode.State?.OnEnter();

            OnStateEntered?.Invoke(currentNode.State);
            OnStateChanged?.Invoke(previous, currentNode.State, null);

            Logger?.Invoke($"[StateMachine] {previous?.GetType().Name ?? "null"} -> {currentNode.State?.GetType().Name ?? "null"} (SetState)");
        }

        void ChangeState(Transition transition)
        {
            var nextState = transition.To;
            if (currentNode != null && nextState == currentNode.State)
                return;

            var previousState = currentNode?.State;
            var nextNode      = GetOrAddNode(nextState);

            previousState?.OnExit();
            OnStateExited?.Invoke(previousState);

            nextNode.State?.OnEnter();
            OnStateEntered?.Invoke(nextNode.State);

            currentNode = nextNode;

            OnStateChanged?.Invoke(previousState, nextNode.State, transition);

            var fromName = previousState != null ? previousState.GetType().Name : "null";
            var toName   = nextNode.State != null ? nextNode.State.GetType().Name : "null";
            var transStr = string.IsNullOrEmpty(transition.Name)
                ? transition.GetType().Name
                : transition.Name;
            Logger?.Invoke($"[StateMachine] {fromName} -> {toName} via {transStr} (prio {transition.Priority})");
        }

        //========================
        // 전이 등록 (Priority 포함)
        //========================

        public void AddTransition<T>(IState from, IState to, T condition, int priority = 0, string name = null)
        {
            GetOrAddNode(from).AddTransition(GetOrAddNode(to).State, condition, priority, name);
        }

        public void AddAnyTransition<T>(IState to, T condition, int priority = 0, string name = null)
        {
            var t = new Transition<T>(GetOrAddNode(to).State, condition)
            {
                Priority = priority,
                Name     = name
            };
            anyTransitions.Add(t);
        }

        //========================
        // 전이 선택 (우선순위)
        //========================

        Transition GetTransition()
        {
            Transition winner   = null;
            int        bestPrio = int.MinValue;

            // AnyState 전이 먼저
            foreach (var t in anyTransitions)
            {
                if (t.Evaluate() && t.Priority >= bestPrio)
                {
                    bestPrio = t.Priority;
                    winner   = t;
                }
            }

            if (currentNode == null)
                return winner;

            // 현재 상태 전이
            foreach (var t in currentNode.Transitions)
            {
                if (t.Evaluate() && t.Priority >= bestPrio)
                {
                    bestPrio = t.Priority;
                    winner   = t;
                }
            }

            return winner;
        }

        StateNode GetOrAddNode(IState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var type = state.GetType();
            if (!nodes.TryGetValue(type, out var node))
            {
                node = new StateNode(state);
                nodes[type] = node;
            }

            return node;
        }

        //========================
        // 내부 노드
        //========================

        class StateNode
        {
            public IState State { get; }
            public HashSet<Transition> Transitions { get; }

            public StateNode(IState state)
            {
                State       = state;
                Transitions = new HashSet<Transition>();
            }

            public void AddTransition<T>(IState to, T predicate, int priority, string name)
            {
                var t = new Transition<T>(to, predicate)
                {
                    Priority = priority,
                    Name     = name
                };
                Transitions.Add(t);
            }
        }

        //========================
        // 디버그/시각화용 스냅샷 API
        //========================

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
            public int    Priority;
            public string Name;
        }

        public void GetDebugSnapshot(
            List<StateInfo> statesOut,
            List<TransitionInfo> transitionsOut)
        {
            statesOut.Clear();
            transitionsOut.Clear();

            foreach (var kv in nodes)
            {
                var node = kv.Value;
                statesOut.Add(new StateInfo
                {
                    Name     = node.State.GetType().Name,
                    Instance = node.State
                });

                foreach (var t in node.Transitions)
                {
                    var condType = GetConditionLabel(t);

                    transitionsOut.Add(new TransitionInfo
                    {
                        From          = node.State.GetType().Name,
                        To            = t.To.GetType().Name,
                        ConditionType = condType,
                        Priority      = t.Priority,
                        Name          = t.Name
                    });
                }
            }

            foreach (var t in anyTransitions)
            {
                var condType = GetConditionLabel(t);

                transitionsOut.Add(new TransitionInfo
                {
                    From          = "Any",
                    To            = t.To.GetType().Name,
                    ConditionType = condType,
                    Priority      = t.Priority,
                    Name          = t.Name
                });

            }
        }
        static string GetConditionLabel(Transition t)
        {
            // Transition<T> 안의 "condition" 필드를 reflection으로 꺼냄
            var type  = t.GetType();
            var field = type.GetField("condition",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var condObj = field?.GetValue(t);
            if (condObj == null)
                return type.Name;

            var typeName = condObj.GetType().Name;

            // 제네릭이면 `1 같은 suffix 제거
            var backtick = typeName.IndexOf('`');
            if (backtick > 0)
                typeName = typeName[..backtick];

            // 자주 쓰는 Predicate들은 짧고 보기 좋게 치환
            return typeName switch
            {
                "BoolParameterPredicate" => "BoolParam",
                "TriggerPredicate"       => "Trigger",
                "DelegatePredicate"      => "Func",
                "AndPredicate"           => "AND",
                "OrPredicate"            => "OR",
                "NotPredicate"           => "NOT",
                _                        => typeName
            };
        }

    }
}
