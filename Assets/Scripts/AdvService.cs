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
    }
}
