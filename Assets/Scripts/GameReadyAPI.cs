using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using YG;

namespace Assets.Scripts
{
    public static class GameReadyAPI
    {
        private static bool _usedGameReadyAPI = false;

        public static void SafeStartGame()
        {
            if (_usedGameReadyAPI) return;

            YG2.GameReadyAPI();
            _usedGameReadyAPI = true;
        }


        public static async UniTask SafeGameplayStart()
        {
            using var errorCts = new CancellationTokenSource();
            void ErrorAction() => errorCts.Cancel();
            YG2.onErrorAnyAdv += ErrorAction;

            // Таймаут 5 секунд – защита от зависания
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(errorCts.Token, timeoutCts.Token);

            try
            {
                // Ждём, пока реклама перестанет показываться
                await UniTask.WaitUntil(() => !YG2.nowAdsShow, cancellationToken: linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Ожидание рекламы прервано (ошибка или таймаут). Продолжаем запуск.");
            }
            finally
            {
                YG2.onErrorAnyAdv -= ErrorAction;
                YG2.GameplayStart();
                Debug.Log("START GAMEPLAY");
            }
        }
    }
}
