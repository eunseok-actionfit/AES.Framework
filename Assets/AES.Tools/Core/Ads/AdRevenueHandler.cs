
using Singular;
using UnityEngine;


public class AdRevenueHandler : MonoBehaviour
{
    void Start()
    {
        // Attach callbacks based on the ad format(s) you are using
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
    }

    private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        double revenue = adInfo.Revenue;

        if (revenue > 0)
        {
            // Create a SingularAdData object with relevant information
            SingularAdData adData = new SingularAdData(
                "Applovin",
                "USD",
                revenue);

            // Send ad revenue data to Singular
            SingularSDK.AdRevenue(adData);
        }
        else
        {
            Debug.LogError("Failed to parse valid revenue value from ad info or revenue is not greater than 0");
        }
    }

    void OnDestroy()
    {
        // Detach callbacks to prevent memory leaks
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnAdRevenuePaidEvent;
        MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnAdRevenuePaidEvent;
    }
}