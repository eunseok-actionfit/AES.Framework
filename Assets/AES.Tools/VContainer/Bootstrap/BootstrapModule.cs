using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;


namespace AES.Tools.VContainer.Bootstrap
{
    /// <summary>
    /// 부트스트랩 모듈 베이스.
    /// 세이브/로거/SDK 초기화용 ScriptableObject 들이 상속해서 사용.
    /// </summary>
    public abstract class BootstrapModule : ScriptableObject
    {
        /// <param name="rootScope">
        /// Root LifetimeScope 인스턴스 (없으면 null).
        /// DI 컨테이너 쓰고 싶으면 rootScope.Container 사용.
        /// </param>
        public abstract UniTask Initialize(LifetimeScope rootScope);
    }
}