// Shared.cs
using System;
using UnityEngine;


namespace AES.Tools.Shared   // 프로젝트 네임스페이스에 맞게
{
    public enum SharedScope
    {
        GameObject,
        ParentAndChildren,
        Scene
    }

    /// <summary>
    /// 인스펙터에서 공유해서 쓰는 타입 기반 컨테이너.
    /// VM과는 완전히 분리된 Unity 전용 레이어.
    /// </summary>
    [Serializable]
    public class Shared<T>
    {
        [SerializeField] private T value;
        [SerializeField] private bool isSource;
        [SerializeField] private SharedScope scope = SharedScope.GameObject;
        [SerializeField] private string groupId = "";

        public T Value
        {
            get => value;
            set => this.value = value;
        }

        public bool IsSource
        {
            get => isSource;
            set => isSource = value;
        }

        public SharedScope Scope
        {
            get => scope;
            set => scope = value;
        }

        /// <summary>
        /// 비우면 "필드 이름"을 그룹키로 사용.
        /// </summary>
        public string GroupId
        {
            get => groupId;
            set => groupId = value;
        }

        public bool HasCustomGroup => !string.IsNullOrEmpty(groupId);
    }
}