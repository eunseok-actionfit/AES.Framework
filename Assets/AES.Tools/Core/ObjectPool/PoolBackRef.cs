using UnityEngine;


namespace AES.Tools
{
    /// <summary>
    /// 소유 풀 참조를 게임오브젝트에 부착한다.<br/>
    /// `ReturnToOwner`가 안전하게 반환할 수 있게 한다.
    /// </summary>
    /// <remarks>
    /// 런타임에만 참조를 설정한다.<br/>
    /// 프리팹에는 기본으로 붙이지 않는다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PoolBackRef : MonoBehaviour
    {
        /// <summary>
        /// 이 오브젝트를 소유한 풀 참조.
        /// </summary>
        public IGameObjectPool OwnerPool { get; private set; }
        

        [System.NonSerialized] IPoolable[] _poolables;

        /// <summary>
        /// 소유 풀을 설정한다.
        /// </summary>
        /// <param name="owner">소유 풀 인스턴스</param>
        public void SetOwner(IGameObjectPool owner)
        {
            OwnerPool = owner;
            // 소유지정 시 1회 초기화. 프리팹에는 안 붙임.
            InitPoolablesIfNeeded();
        }

        // 외부에서 필요 시 갱신 가능(드물게 동적 AddComponent 한 경우)
        public void RefreshPoolables() => _poolables = GetComponents<IPoolable>();

        public IPoolable[] GetPoolables()
        {
            InitPoolablesIfNeeded();
            return _poolables;
        }

        void InitPoolablesIfNeeded()
        {
            if (_poolables == null) _poolables = GetComponents<IPoolable>();
        }
    }

    /// <summary>
    /// 소유 풀로 반환하거나 파괴하는 유틸리티.<br/>
    /// `PoolBackRef`를 사용해 소유 풀을 찾는다.
    /// </summary>
    public static class PoolReturnHelper
    {
        /// <summary>
        /// 소유 풀로 반환을 시도한다.<br/>
        /// 실패하면 오브젝트를 파괴한다.
        /// </summary>
        /// <param name="go">대상 게임오브젝트</param>
        /// <returns><c>true</c>면 풀로 반환, <c>false</c>면 파괴됨</returns>
        public static bool ReturnToOwner(GameObject go)
        {
            if (!go) return false;
            var br = go.GetComponent<PoolBackRef>();

            if (br?.OwnerPool != null)
            {
                br.OwnerPool.Return(go);
                return true;
            }

            Object.Destroy(go);
            return false;
        }
    }

    /// <summary>
    /// 풀 반환 확장 메서드.<br/>
    /// `GameObject`와 `Component`에 적용한다.
    /// </summary>
    public static class PoolReturnExtensions
    {
        /// <summary>
        /// 소유 풀로 반환을 시도한다.
        /// </summary>
        /// <param name="go">대상 게임오브젝트</param>
        /// <returns>반환 성공 여부</returns>
        public static bool ReturnToOwner(this GameObject go) => PoolReturnHelper.ReturnToOwner(go);

        /// <summary>
        /// 소유 풀로 반환을 시도한다.
        /// </summary>
        /// <param name="c">대상 컴포넌트</param>
        /// <returns>반환 성공 여부</returns>
        public static bool ReturnToOwner(this Component c) => c && c.gameObject.ReturnToOwner();
    }
}