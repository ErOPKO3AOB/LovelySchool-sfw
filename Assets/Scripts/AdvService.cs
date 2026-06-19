using VContainer.Unity;
using YG;

namespace Assets.Scripts
{
    public class AdvService : IInitializable
    {
        private bool _advDisabled = false;

        public void Initialize()
        {
            YG2.StickyAdActivity(!_advDisabled);
        }

        public void ShowInterstitial()
        {
            if (_advDisabled) return;
            YG2.InterstitialAdvShow();
        }
    }
}
