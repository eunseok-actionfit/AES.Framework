using System;
using UnityEngine;

namespace AES.Tools
{
    public enum ContextNameMode
    {
        TypeName,
        GameObjectName,
        Custom
    }

    [DefaultExecutionOrder(-1)]
    public abstract class DataContextBase : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("ContextName을 어떤 방식으로 결정할지 선택")]
        private ContextNameMode nameMode = ContextNameMode.TypeName;

        [SerializeField]
        [ShowIf(nameof(nameMode), ContextNameMode.Custom)]
        private string customName;

        public string ContextName
        {
            get
            {
                switch (nameMode)
                {
                    case ContextNameMode.GameObjectName:
                        return gameObject.name;

                    case ContextNameMode.TypeName:
                        return GetType().Name;

                    case ContextNameMode.Custom:
                        return string.IsNullOrEmpty(customName)
                            ? gameObject.name
                            : customName;

                    default:
                        return gameObject.name;
                }
            }
        }

        /// <summary>
        /// 이 Context가 다루는 ViewModel의 타입.
        /// 에디터/런타임 공통으로 사용.
        /// </summary>
        public abstract Type ViewModelType { get; }

        public object ViewModel { get; protected set; }

        protected virtual void Awake()
        {
            ViewModel ??= CreateViewModel();
        }

        /// <summary>
        /// 런타임용 ViewModel 생성.
        /// </summary>
        protected abstract object CreateViewModel();

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 Path 드롭다운을 만들 때 사용할 ViewModel 타입.
        /// 기본 구현은 ViewModelType 그대로 사용.
        /// (Editor 스크립트에서 ctx.GetViewModelType() 으로 사용)
        /// </summary>
        public virtual Type GetViewModelType()
        {
            return ViewModelType;
        }

        /// <summary>
        /// 에디터에서 딕셔너리 키 등 인스턴스 정보가 필요할 때 사용할 디자인타임 ViewModel.
        /// 기본은 null (== 인스턴스 없음, 타입 정보만 사용).
        /// 필요하면 파생 클래스에서 override해서 new ViewModel(mockData) 리턴.
        /// (Editor 스크립트에서 ctx.GetDesignTimeViewModel() 으로 사용)
        /// </summary>
        public virtual object GetDesignTimeViewModel()
        {
            return null;
        }
#endif
    }
}
