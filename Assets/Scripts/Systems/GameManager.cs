using UnityEngine;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public int currentDifficulty = 0;

    private MinesweeperGameManager _minesweeperManager;

    //ads
#if UNITY_ANDROID
    private string _rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
    private string _rewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
    private string _rewardedAdUnitId = "unused";
#endif

    private RewardedAd _rewardedAd;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        MobileAds.Initialize(initStatus => {
            // Initialization callback
            Debug.Log("Google Mobile Ads initialized.");
        });
    }


    private void Start()
    {
        LoadRewardedAd();
    }
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }




    //ads

    private void LoadRewardedAd()
    {
        // Destroy any existing ad before loading a new one
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        Debug.Log("Loading a new rewarded ad...");

        // Create an ad request using the correct method
        AdRequest adRequest = new AdRequest
        {
            Keywords = null, // Optional: You can set targeting keywords
            Extras = null, // Optional: Add extra targeting data
            MediationExtras = null // Optional: Mediation-specific options
        };

        // Configure COPPA (Child-Directed Treatment)
        RequestConfiguration requestConfiguration = new RequestConfiguration
        {
            TagForChildDirectedTreatment = TagForChildDirectedTreatment.True, // Ensure child-friendly ads
            MaxAdContentRating = MaxAdContentRating.G // Restrict to G-rated ads
        };

        MobileAds.SetRequestConfiguration(requestConfiguration);

        // Load the rewarded ad
        RewardedAd.Load(_rewardedAdUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Rewarded ad failed to load: " + error);
                return;
            }

            Debug.Log("Rewarded ad loaded successfully.");

            _rewardedAd = ad;

            // Register event handlers
            RegisterEventHandlers(_rewardedAd);
        });
    }

    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Called when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log($"Rewarded ad paid {adValue.Value} {adValue.CurrencyCode}");
        };

        // Called when an impression is recorded.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded ad recorded an impression.");
        };

        // Called when a click is recorded.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded ad was clicked.");
        };

        // Called when the ad opens full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad full screen content opened.");
        };

        // Called when the ad closed.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad full screen content closed.");

            // Since this ad is "one-time use," load a new ad for the next time.
            LoadRewardedAd();
        };

        // Called when the ad fails to open.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed: " + error);
            if (_minesweeperManager != null)
            {
                _minesweeperManager.ShowErrorPopup("Ad failed to open. Please try again later.");
            }
            LoadRewardedAd();
        };
    }


    public void SetMinesweeperManagerReference(MinesweeperGameManager mgr)
    {
        _minesweeperManager = mgr;
    }

    public void OnHintButtonClicked()
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("User rewarded!");
                if (_minesweeperManager != null)
                {
                    _minesweeperManager.OnHintButtonClicked();
                }
            });
        }
        else
        {
            // If the ad isn't ready, show a popup or fallback
            Debug.Log("Ad not ready");
            if (_minesweeperManager != null)
            {
                _minesweeperManager.ShowErrorPopup("Ads are not available right now. Please try again later.");
            }
        }
    }



}
