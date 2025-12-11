using System;

namespace AES.Tools
{
    /// <summary>
    /// Animator의 bool 파라미터 느낌.
    /// 순수 값만 들고 있고, Predicate로 래핑해서 사용.
    /// </summary>
    public sealed class BoolParameter
    {
        public bool Value { get; set; }

        public BoolParameter(bool initial = false)
        {
            Value = initial;
        }
    }

    /// <summary>
    /// Animator의 trigger 파라미터 느낌.
    /// Fire() 한 번 하면, Consume()이 true 한 번 반환 후 자동 리셋.
    /// </summary>
    public sealed class TriggerParameter
    {
        bool fired;

        public void Fire() => fired = true;

        public void Reset() => fired = false;

        public bool Consume()
        {
            if (!fired)
                return false;

            fired = false;
            return true;
        }
    }

    /// <summary>
    /// "bool 파라미터 == expected"를 검사하는 Predicate.
    /// </summary>
    public sealed class BoolParameterPredicate : IPredicate
    {
        readonly BoolParameter _parameter;
        readonly bool _expected;

        public BoolParameterPredicate(BoolParameter parameter, bool expected = true)
        {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _expected  = expected;
        }

        public bool Evaluate() => _parameter.Value == _expected;
    }

    /// <summary>
    /// TriggerParameter.Consume()을 한 번 호출하는 Predicate.
    /// </summary>
    public sealed class TriggerPredicate : IPredicate
    {
        readonly TriggerParameter _parameter;

        public TriggerPredicate(TriggerParameter parameter)
        {
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public bool Evaluate() => _parameter.Consume();
    }
}