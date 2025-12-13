// SharedVmBindingInfo.cs
using System;
using UnityEngine;


namespace AES.Tools.Shared
{
    public enum VmBindingDirection
    {
        VmToShared,
        SharedToVm,
        TwoWay
    }

    [Serializable]
    public sealed class SharedVmBindingInfo
    {
        [Tooltip("BindingContext에서 접근할 VM 경로 (예: \"Player.Hp\")")]
        public string vmPath;

        public VmBindingDirection direction = VmBindingDirection.VmToShared;
    }
}