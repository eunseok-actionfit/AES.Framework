using System;
using UnityEngine;

namespace AES.Tools
{
    public enum BindingMode
    {
        Constant,   // 상수 값 ("Hello")
        Path,       // ViewModel 경로 ("Player.Level")
        Reference,  // UnityEngine.Object 참조
        Provider    // ScriptableObject 기반 제공자
    }

    [Serializable]
    public struct DataBinding
    {
        [Header("Source")]
        public BindingMode mode;

        // Mode == Path
        public string path;

        // Mode == Constant
        public string constant;

        // Mode == Reference
        public UnityEngine.Object reference;

        // Mode == Provider
        public ScriptableObject provider;

        // 프리팹 등에서 Path 드롭다운을 위해 사용할 디자인타임 ViewModel 타입
        [SerializeField, Tooltip("디자인 타임에 Path 후보를 만들 때 사용할 ViewModel 타입(AssemblyQualifiedName)")]
        private string designTimeViewModelTypeName;

        public string DesignTimeViewModelTypeName
        {
            get => designTimeViewModelTypeName;
            set => designTimeViewModelTypeName = value;
        }

        public Type DesignTimeViewModelType
        {
            get
            {
                if (string.IsNullOrEmpty(designTimeViewModelTypeName))
                    return null;
                return Type.GetType(designTimeViewModelTypeName);
            }
            set => designTimeViewModelTypeName =
                value?.AssemblyQualifiedName;
        }
    }
}