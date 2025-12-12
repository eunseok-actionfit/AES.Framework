using System;

namespace AES.Tools
{
    /// <summary>
    /// 불리언 값을 저장하는 상태 머신 파라미터.<br/>
    /// 값 비교 기반 전이 조건에 사용된다.
    /// </summary>
    public sealed class BoolParameter
    {
        // 현재 파라미터 값
        public bool Value;

        /// <summary>
        /// 초기값을 지정해 파라미터를 생성한다.
        /// </summary>
        /// <param name="initial">초기 불리언 값.</param>
        public BoolParameter(bool initial = false)
            => Value = initial;
    }

    /// <summary>
    /// 1회성 이벤트를 표현하는 트리거 파라미터.<br/>
    /// 소비되면 자동으로 리셋된다.
    /// </summary>
    public sealed class TriggerParameter
    {
        // 트리거 발생 여부
        private bool fired;

        /// <summary>
        /// 트리거를 발생시킨다.<br/>
        /// 다음 평가에서 한 번 소비된다.
        /// </summary>
        public void Fire()
            => fired = true;

        /// <summary>
        /// 트리거를 강제로 초기화한다.
        /// </summary>
        public void Reset()
            => fired = false;

        /// <summary>
        /// 트리거를 소비한다.<br/>
        /// 발생 상태면 한 번만 <c>true</c>를 반환한다.
        /// </summary>
        /// <returns>이번 호출에서 소비되었으면 <c>true</c>.</returns>
        public bool Consume()
        {
            if (!fired)
                return false;

            fired = false;
            return true;
        }
    }

    /// <summary>
    /// `BoolParameter` 값이 기대값과 일치하는지 평가한다.<br/>
    /// 값 비교 기반 전이에 사용된다.
    /// </summary>
    public sealed class BoolParameterPredicate : IPredicate
    {
        // 평가 대상 파라미터
        private readonly BoolParameter param;

        // 기대하는 값
        private readonly bool expected;

        /// <summary>
        /// 파라미터와 기대값을 지정해 생성한다.
        /// </summary>
        /// <param name="param">평가할 불리언 파라미터.</param>
        /// <param name="expected">기대 값.</param>
        public BoolParameterPredicate(BoolParameter param, bool expected)
        {
            this.param = param;
            this.expected = expected;
        }

        /// <summary>
        /// 현재 값이 기대값과 같은지 평가한다.
        /// </summary>
        /// <returns>일치하면 <c>true</c>, 아니면 <c>false</c>.</returns>
        public bool Evaluate()
            => param.Value == expected;
    }

    /// <summary>
    /// `TriggerParameter`를 1회 소비하는 전이 조건.<br/>
    /// 이벤트 기반 전이에 사용된다.
    /// </summary>
    public sealed class TriggerPredicate : IPredicate
    {
        // 평가 대상 트리거
        private readonly TriggerParameter trigger;

        /// <summary>
        /// 대상 트리거를 지정해 생성한다.
        /// </summary>
        /// <param name="trigger">평가할 트리거 파라미터.</param>
        public TriggerPredicate(TriggerParameter trigger)
            => this.trigger = trigger;

        /// <summary>
        /// 트리거를 소비하여 조건을 평가한다.
        /// </summary>
        /// <returns>이번 평가에서 트리거가 발생했으면 <c>true</c>.</returns>
        public bool Evaluate()
            => trigger.Consume();
    }

    /// <summary>
    /// 파라미터를 전이 조건으로 변환하는 확장 메서드 모음.<br/>
    /// 선언부에서 가독성을 높이기 위해 제공된다.
    /// </summary>
    public static partial class Predicates
    {
        /// <summary>
        /// 파라미터가 <c>true</c>인지 평가하는 조건을 생성한다.
        /// </summary>
        /// <param name="p">대상 불리언 파라미터.</param>
        /// <returns>참 비교 조건.</returns>
        public static IPredicate IsTrue(this BoolParameter p)
            => new BoolParameterPredicate(p, true);

        /// <summary>
        /// 파라미터가 <c>false</c>인지 평가하는 조건을 생성한다.
        /// </summary>
        /// <param name="p">대상 불리언 파라미터.</param>
        /// <returns>거짓 비교 조건.</returns>
        public static IPredicate IsFalse(this BoolParameter p)
            => new BoolParameterPredicate(p, false);

        /// <summary>
        /// 트리거를 1회 소비하는 조건으로 변환한다.
        /// </summary>
        /// <param name="p">대상 트리거 파라미터.</param>
        /// <returns>트리거 기반 조건.</returns>
        public static IPredicate AsTrigger(this TriggerParameter p)
            => new TriggerPredicate(p);
    }
}
