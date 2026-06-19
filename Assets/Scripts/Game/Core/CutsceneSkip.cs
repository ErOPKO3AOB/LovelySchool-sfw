using UnityEngine;
using UnityEngine.UI;
using YG;
using PlayerPrefs = RedefineYG.PlayerPrefs;

namespace Game
{
    public class CutsceneSkip : MonoBehaviour
    {
        [SerializeField] private Button _skipButton;
        [SerializeField] private AnimationClip _animation;
        [SerializeField] private SceneLoader _sceneLoader;

        private bool _canSkip = false;

        private void Start()
        {
            _canSkip = PlayerPrefs.GetInt(_animation.name, 0) == 1;

            _skipButton.gameObject.SetActive(false);
            if (_canSkip)
            {
                Invoke(nameof(AllowSkip), 1.5f);
            }

            Invoke(nameof(AnimationEnd), _animation.length);
        }

        private void AllowSkip()
        {
            _skipButton.gameObject.SetActive(true);

            _skipButton.onClick.AddListener(() =>
            {
                _skipButton.enabled = false;
                CancelInvoke(nameof(AnimationEnd));
                AnimationEnd();
            });

            if (YG2.envir.device == YG2.Device.Desktop)
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void AnimationEnd()
        {
            PlayerPrefs.SetInt(_animation.name, 1);
            YG2.SaveProgress();
            _sceneLoader.LoadScene();
        }
    }
}