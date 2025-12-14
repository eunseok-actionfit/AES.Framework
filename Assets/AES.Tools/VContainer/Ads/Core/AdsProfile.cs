using System;
using UnityEngine;


namespace AES.Tools.VContainer {
    public enum AdsEnvironment { Development, Staging, Production }

    [Serializable]
    public class PlacementConfig
    {
        public AdNetworkType network;

        public string adUnitId;
    }

    [Serializable]
    public class AdsProfile
    {
        public AdsEnvironment environment;
        public RuntimePlatform platform;
        public PlacementConfig appOpen;
        public PlacementConfig banner;
        public PlacementConfig interstitial;
        public PlacementConfig rewarded;

    }
}