using UnityEngine;
using GoogleMobileAds.Api;
using System.IO;

public class RewardedAdManager : MonoBehaviour
{
    private RewardedAd rewardedAd;

    // Replace with your real Ad Unit ID
    private string rewardedAdUnitId = "ca-app-pub-8942564867686946~7768731267";

   /* void Start()
    {
        // Initialize and load the rewarded ad
        this.rewardedAd = new RewardedAd(rewardedAdUnitId);

        // Attach event handlers
        this.rewardedAd.OnAdLoaded += HandleRewardedAdLoaded;
        this.rewardedAd.OnAdFailedToLoad += HandleRewardedAdFailedToLoad;
        this.rewardedAd.OnAdOpening += HandleRewardedAdOpening;
        this.rewardedAd.OnAdFailedToShow += HandleRewardedAdFailedToShow;
        this.rewardedAd.OnUserEarnedReward += HandleUserEarnedReward;
        this.rewardedAd.OnAdClosed += HandleRewardedAdClosed;

        // Create an empty ad request
        AdRequest request = new AdRequest.Builder().Build();

        // Load the rewarded ad
        this.rewardedAd.LoadAd(request);
    }

    // Example method to show the ad when a button is clicked, for example
    public void ShowRewardedAd()
    {
        if (this.rewardedAd.IsLoaded())
        {
            this.rewardedAd.Show();
        }
        else
        {
            Debug.Log("Rewarded ad is not loaded yet.");
        }
    }

    #region Rewarded Ad Callback Handlers

    public void HandleRewardedAdLoaded(object sender, System.EventArgs args)
    {
        Debug.Log("Rewarded Ad Loaded");
    }

    public void HandleRewardedAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        Debug.Log("Rewarded Ad Failed To Load: " + args.LoadAdError.GetMessage());
    }

    public void HandleRewardedAdOpening(object sender, System.EventArgs args)
    {
        Debug.Log("Rewarded Ad Opened");
    }

    public void HandleRewardedAdFailedToShow(object sender, AdErrorEventArgs args)
    {
        Debug.Log("Rewarded Ad Failed To Show: " + args.AdError.GetMessage());
    }

    // This is where you give the reward to the player
    public void HandleUserEarnedReward(object sender, Reward args)
    {
        Debug.Log("User Earned Reward: " + args.Amount.ToString() + " " + args.Type);
        // e.g. Give the player coins, or unlock some content:
        // playerCoins += (int)args.Amount;
    }

    public void HandleRewardedAdClosed(object sender, System.EventArgs args)
    {
        Debug.Log("Rewarded Ad Closed");

        // Optionally, load another rewarded ad:
        this.rewardedAd = new RewardedAd(rewardedAdUnitId);
        this.rewardedAd.OnAdLoaded += HandleRewardedAdLoaded;
        this.rewardedAd.OnAdFailedToLoad += HandleRewardedAdFailedToLoad;
        this.rewardedAd.OnAdOpening += HandleRewardedAdOpening;
        this.rewardedAd.OnAdFailedToShow += HandleRewardedAdFailedToShow;
        this.rewardedAd.OnUserEarnedReward += HandleUserEarnedReward;
        this.rewardedAd.OnAdClosed += HandleRewardedAdClosed;

        AdRequest request = new AdRequest.Builder().Build();
        this.rewardedAd.LoadAd(request);
    }

    #endregion*/
}
