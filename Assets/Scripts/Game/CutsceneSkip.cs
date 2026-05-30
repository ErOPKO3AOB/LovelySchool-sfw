using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

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

            _skipButton.onClick.AddListener(() =>
            {
                if (_canSkip)
                {
                    _skipButton.enabled = false;
                    CancelInvoke(nameof(AnimationEnd));
                    AnimationEnd();
                }
            });

            Invoke(nameof(AnimationEnd), _animation.length);
        }

        private void AnimationEnd()
        {
            PlayerPrefs.SetInt(_animation.name, 1);
            _sceneLoader.LoadScene();
        }
    }
}