using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif


namespace AES.Tools
{

    public interface IPredicate
    {
        bool Evaluate();
    }

    public class And : IPredicate
    {
        [SerializeField] List<IPredicate> rules = new List<IPredicate>();
        public bool Evaluate() => rules.All(r => r.Evaluate());
    }

    public class Or : IPredicate
    {
        [SerializeField] List<IPredicate> rules = new List<IPredicate>();
        public bool Evaluate() => rules.Any(r => r.Evaluate());
    }

    public class Not : IPredicate
    {
#if ODIN_INSPECTOR
        [LabelWidth(80)]
#endif
        [SerializeField] IPredicate rule;
        public bool Evaluate() => !rule.Evaluate();
    }
}