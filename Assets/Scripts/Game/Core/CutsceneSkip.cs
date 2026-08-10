using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YG;
using PlayerPrefs = RedefineYG.PlayerPrefs;

namespace Game
{
    public class CutsceneSkip : MonoBehaviour
    {
        [SerializeField] private Button _skipRequestButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private AnimationClip _animation;
        [SerializeField] private SceneLoader _sceneLoader;

        [SerializeField] private float _fadeOutDuration = 0.4f;

        private readonly List<Graphic> _graphics = new();
        private readonly List<float> _originalAlphas = new();
        private Coroutine _fadeCoroutine;

        private void Start()
        {
            GameReadyAPI.SafeStartGame();
            YG2.GameplayStop();

            YG2.SwitchLanguage(PlayerPrefs.GetString("Language", YG2.lang));

            if (YG2.envir.device == YG2.Device.Desktop)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            // Собираем Image кнопки + все Graphic (Text/TextMeshPro и т.д.) в детях
            CollectGraphics(_skipButton.transform);

            _skipRequestButton.onClick.AddListener(ShowSkip);

            _skipButton.onClick.AddListener(() =>
            {
                _skipRequestButton.enabled = false;
                _skipButton.enabled = false;

                CancelInvoke(nameof(AnimationEnd));
                AnimationEnd();
            });

            Invoke(nameof(AnimationEnd), _animation.length);

            Invoke(nameof(ShowSkip), SceneManager.GetActiveScene().name == "EndCutscene" ? 3 : 0);
        }

        private void CollectGraphics(Transform root)
        {
            var graphics = root.GetComponentsInChildren<Graphic>(true);

            foreach (var g in graphics)
            {
                _graphics.Add(g);
                _originalAlphas.Add(g.color.a);
            }
        }

        private void ShowSkip()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            CancelInvoke(nameof(StartFadeOut));

            // Резко возвращаем стартовую прозрачность (не 1)
            _skipButton.gameObject.SetActive(true);
            SetAlphasToOriginal();
            _skipButton.interactable = true;

            Invoke(nameof(StartFadeOut), 1.5f);
        }

        private void StartFadeOut()
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            float time = 0f;
            _skipButton.interactable = false;

            // Запоминаем текущие альфы на момент начала исчезновения
            var startAlphas = new float[_graphics.Count];
            for (int i = 0; i < _graphics.Count; i++)
                startAlphas[i] = _graphics[i].color.a;

            while (time < _fadeOutDuration)
            {
                time += Time.deltaTime;
                float t = time / _fadeOutDuration;

                for (int i = 0; i < _graphics.Count; i++)
                {
                    Color c = _graphics[i].color;
                    c.a = Mathf.Lerp(startAlphas[i], 0f, t);
                    _graphics[i].color = c;
                }

                yield return null;
            }

            // Полностью прозрачные
            for (int i = 0; i < _graphics.Count; i++)
            {
                Color c = _graphics[i].color;
                c.a = 0f;
                _graphics[i].color = c;
            }

            _skipButton.gameObject.SetActive(false);
            _fadeCoroutine = null;
        }

        private void SetAlphasToOriginal()
        {
            for (int i = 0; i < _graphics.Count; i++)
            {
                Color c = _graphics[i].color;
                c.a = _originalAlphas[i];
                _graphics[i].color = c;
            }
        }

        private void AnimationEnd()
        {
            YG2.SaveProgress();
            _sceneLoader.LoadScene();
        }

        private void OnDestroy()
        {
            _skipRequestButton.onClick.RemoveAllListeners();
            _skipButton.onClick.RemoveAllListeners();
        }
    }
}