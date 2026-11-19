using System;
using UniWebKit;
using UnityEngine;
using VContainer.Unity;

namespace Assets.Scripts
{
    public class AdvService : IInitializable
    {
        private const double InterstitialIntervalSeconds = 120;
        private static double _nextInterstitialAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => DeferInterstitial();

        private static void DeferInterstitial() =>
            _nextInterstitialAt = Time.realtimeSinceStartupAsDouble + InterstitialIntervalSeconds;

        public void Initialize() => UniWebBridge.Ads.ShowStickyBanner();

        public void BeforeManualRestart(Action restart)
        {
            var ads = UniWebBridge.Ads;
            if (Time.realtimeSinceStartupAsDouble < _nextInterstitialAt || !ads.IsInterstitialReady)
            {
                restart();
                return;
            }

            DeferInterstitial();
            RunFullscreenAd((opened, closed) => ads.ShowInterstitial(opened, closed, LogAdError), _ => restart());
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
            RunFullscreenAd(
                (opened, closed) => ads.ShowRewarded(onReward, opened, closed, onError, placementId),
                onClosed);
        }

        private static void RunFullscreenAd(Action<Action, Action<bool>> show, Action<bool> onClosed)
        {
            bool opened = false;
            bool completed = false;
            float previousTimeScale = Time.timeScale;
            bool previousAudioPause = AudioListener.pause;

            void OnOpened()
            {
                if (completed || opened) return;
                opened = true;
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }

            void OnClosed(bool success)
            {
                if (completed) return;
                completed = true;
                if (opened)
                {
                    Time.timeScale = previousTimeScale;
                    AudioListener.pause = previousAudioPause;
                    DeferInterstitial();
                }
                onClosed?.Invoke(success);
            }

            try { show(OnOpened, OnClosed); }
            catch (Exception error) when (!completed)
            {
                Debug.LogException(error);
                OnClosed(false);
            }
        }

        private static void LogAdError(string error) => Debug.LogWarning("[Ads] " + error);
    }
}
