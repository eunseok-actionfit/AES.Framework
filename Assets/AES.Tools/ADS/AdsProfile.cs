using System;
using AES.Tools;
using UnityEngine;

public enum AdsEnvironment { Development, Staging, Production }

[Serializable]
public class PlacementConfig
{
    [AesLabelText("광고 네트워크")]
    public AdNetworkType network;

    [AesLabelText("Unit ID")]
    public string adUnitId;
}

[Serializable]
public class AdsProfile
{
    [AesLabelText("환경")]
    public AdsEnvironment environment;

    [AesLabelText("플랫폼")]
    public RuntimePlatform platform;
    
    [AesLabelText("앱 오픈")]
    public PlacementConfig appOpen;
    
    [AesLabelText("전면 광고")]
    public PlacementConfig interstitial;

    [AesLabelText("보상 광고")]
    public PlacementConfig rewarded;

    [AesLabelText("배너 광고")]
    public PlacementConfig banner;
}