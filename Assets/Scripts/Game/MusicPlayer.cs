using Player;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;
using YG;

namespace Game
{
    public class MusicPlayer : MonoBehaviour
    {
        [Inject]
        public void Construct(PlayerFacade playerFacade, GameController gameController)
        {
            _playerFacade = playerFacade;
            _gameController = gameController;
        }

        private PlayerFacade _playerFacade;
        private GameController _gameController;

        [SerializeField] private AudioClip[] _audios;
        [SerializeField, Range(0, 1)] private float _volume = 0.5f;
        [SerializeField] private float _fadeOutDuration = 3f;
        [SerializeField] private AudioMixer _audioMixer;

        private int _lastAudioIndex = -1;
        private AudioSource _audioSource;
        private bool _isGamePaused; // флаг паузы

        private void Start()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("Master")[0];
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0;
            _audioSource.volume = _volume;
            _audioSource.pitch = 1f;
            _playerFacade.Health.OnDead += OnDead;

            _gameController.OnPauseStateChanged += OnPauseStateChanged;
        }

        private void OnPauseStateChanged(bool paused)
        {
            _isGamePaused = paused;

            YG2.PauseGame(paused);

            if (paused)
                _audioSource.Pause();
            else if (!_gameController.DisableMusicToggle.isOn)
                _audioSource.UnPause();
        }

        private void PlayNext()
        {
            if (_gameController.DisableMusicToggle.isOn) return;

            int currentIndex = Random.Range(0, _audios.Length);
            int currentIteration = 0;
            int maxIteration = 10;

            while (currentIndex == _lastAudioIndex && currentIteration < maxIteration)
            {
                currentIteration++;
                currentIndex = Random.Range(0, _audios.Length);
            }

            _audioSource.clip = _audios[currentIndex];
            if(_audioSource.enabled)
            _audioSource.Play();
            _lastAudioIndex = currentIndex;
        }

        private void OnDead(GameObject obj)
        {
            StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            float startVolume = _audioSource.volume;
            float startPitch = _audioSource.pitch;
            float elapsed = 0f;

            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _fadeOutDuration;
                _audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
                _audioSource.pitch = Mathf.Lerp(startPitch, 0f, t);
                yield return null;
            }

            _audioSource.volume = 0f;
            _audioSource.pitch = 0f;
            _audioSource.enabled = false;
        }

        private void Update()
        {
            // Не переключаем треки, пока игра на паузе
            if (_isGamePaused) return;

            if (!_audioSource.isPlaying)
                PlayNext();
        }

        private void OnDestroy()
        {
            if (_playerFacade?.Health != null)
                _playerFacade.Health.OnDead -= OnDead;

            _gameController.OnPauseStateChanged -= OnPauseStateChanged;
        }
    }
}