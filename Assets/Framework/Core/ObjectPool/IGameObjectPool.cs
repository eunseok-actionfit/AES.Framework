using UnityEngine;


namespace AES.Tools
{
    /// <summary>
    /// `GameObject`를 풀로 반환하기 위한 인터페이스.<br/>
    /// 소유 풀을 통해 반환 로직을 캡슐화한다.
    /// </summary>
    public interface IGameObjectPool
    {
        /// <summary>
        /// 객체를 풀로 반환한다.
        /// </summary>
        /// <param name="go">반환할 게임오브젝트</param>
        void Return(GameObject go);
    }
}