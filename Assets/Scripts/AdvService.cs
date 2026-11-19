using System;
using UniWebKit;
using VContainer.Unity;

namespace Assets.Scripts
{
    public class AdvService : IInitializable
    {
        //private bool _advDisabled = false;

        public void Initialize()
        {
            //if (_advDisabled)
            //{
                //UniWebBridge.Ads.HideStickyBanner();
            //}

            //else
            //{
                UniWebBridge.Ads.ShowStickyBanner();
            //}
        }

        public void ShowInterstitial()
        {
            //if (_advDisabled) return;

            UniWebBridge.Ads.ShowInterstitial();
        }

        public void ShowRewarded(Action onReward, Action<bool> onClosed, Action<string> onError, string placementId)
        {
            var ads = UniWebBridge.Ads;
#if UNITY_EDITOR && UNIWEB_VK_GAMES
            if (ads is VKGamesAdvProvider)
            {
                try
                {
                    onReward?.Invoke();
                }
                finally
                {
                    onClosed?.Invoke(true);
                }
                return;
            }
#endif
            ads.ShowRewarded(onReward, onClosed: onClosed, onError: onError, placementId: placementId);
        }
    }
}
