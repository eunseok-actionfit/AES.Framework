namespace Core.Systems.Pooling
{
    /// <summary>
    /// 풀링 가능한 객체 인터페이스
    /// - 대여/반납 시점에 초기화 및 정리 로직을 삽입할 수 있음
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 객체가 풀에서 대여될 때 호출됨
        /// - 예: 상태 초기화, 활성화
        /// </summary>
        void OnRent();

        /// <summary>
        /// 객체가 풀에 반환될 때 호출됨
        /// - 예: 상태 리셋, 비활성화
        /// </summary>
        void OnReturn();
    }
}