using System;
using System.Collections.Generic;
using UnityEngine;


namespace AES.Tools.StateMachine
{
    [CreateAssetMenu(menuName = "FSM/State Machine Graph", fileName = "StateMachineGraph")]
    public sealed class StateMachineGraphAsset : ScriptableObject
    {
        public string machineName;

        [Serializable]
        public class NodeData
        {
            public string  stateName;
            public Vector2 position;
        }

        [Serializable]
        public class TransitionData
        {
            public string fromState;
            public string toState;
            public string label; // conditionType / name / priority 등 표현
        }

        public List<NodeData>      nodes       = new();
        public List<TransitionData> transitions = new();
    }
}