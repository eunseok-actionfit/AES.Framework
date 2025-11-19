using System.Collections.Generic;


namespace Core.Engine.Collections
{
    /// <summary>
    /// 임시 리스트를 재사용하기 위한 내부 유틸리티.<br/>
    /// 빈 <see cref="List{T}"/> 인스턴스를 스택 풀로 관리한다.
    /// </summary>
    /// <typeparam name="T">리스트 요소 타입</typeparam>
    public static class ReusableList<T>
    {
        // 내부 스택 풀
        private static readonly Stack<List<T>> _pool = new();

        /// <summary>
        /// 비어 있는 리스트를 대여한다.<br/>
        /// 없으면 새로 생성한다.
        /// </summary>
        /// <returns>대여된 리스트 인스턴스</returns>
        public static List<T> Rent()
        {
            lock (_pool)
                return _pool.Count > 0 ? _pool.Pop() : new List<T>();
        }

        /// <summary>
        /// 리스트를 초기화하고 풀로 반환한다.
        /// </summary>
        /// <param name="list">반환할 리스트</param>
        public static void Return(List<T> list)
        {
            list.Clear();
            lock (_pool) _pool.Push(list);
        }
    }
}