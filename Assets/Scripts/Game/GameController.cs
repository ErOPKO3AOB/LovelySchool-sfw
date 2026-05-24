using Assets.Scripts.Game;
using Data;
using Input;
using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VContainer;
using YG;
using Random = UnityEngine.Random;

namespace Game
{
    public class GameController : MonoBehaviour
    {
        [Inject]
        public void Construct(Func<Transform, GameObject, Enemy> enemyFactory, PlayerFacade playerFacade, InputReader inputReader, GameSettingsSO gameSettings)
        {
            _enemyFactory = enemyFactory;
            _playerFacade = playerFacade;
            _inputReader = inputReader;
            _gameSettings = gameSettings;
        }

        private Func<Transform, GameObject, Enemy> _enemyFactory;
        private PlayerFacade _playerFacade;
        private InputReader _inputReader;
        private GameSettingsSO _gameSettings;

        [Header("Enemies")]
        [SerializeField] private List<Enemy> _enemies = new();
        [SerializeField] private TMP_Text _enemyCountText;
        [SerializeField] private float _outlineTime = 15f;

        [Header("Rooms")]
        [SerializeField] private Room[] _rooms;
        [SerializeField] private float _messageHoldTime = 6;
        [SerializeField] private TMP_Text _messageText;

        [Header("Death")]
        [SerializeField] private string[] _looseMessages;
        [SerializeField] private Button _restartButton;
        [SerializeField] private TMP_Text _looseText;
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private float _deathDelay = 6f;

        [Header("Pause")]
        [SerializeField] private GameObject _pauseMenuUI;
        [SerializeField] private Toggle _disableMusicToggle;
        [SerializeField] private Slider _sensitivitySlider;

        private const string _monkeysText = "Обезьян осталось:";

        public UnityAction<bool> OnPauseStateChanged;

        private bool _isPaused = false;

        private bool _playerDead = false;

        public bool IsPaused => _isPaused;
        public Toggle DisableMusicToggle => _disableMusicToggle;

        private void Start()
        {
            for (int i = 0; i < _rooms.Length; i++)
            {
                _rooms[i].OnCleaned += (message) => { StartCoroutine(MessageRoutine(message)); };
            }

            _playerFacade.Health.OnDead += OnPlayerDead;

            _inputReader.OnPausePerformed += OnPausePerformed;

            _pauseMenuUI.SetActive(true);

            _sensitivitySlider.onValueChanged.AddListener((value) =>
            {
                _gameSettings.mouseSensitivity = value;
            });

            _pauseMenuUI.SetActive(false);
        }

        private void SetPaused(bool paused)
        {
            if (_playerDead) return;
            _isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            OnPauseStateChanged?.Invoke(paused);

            if (paused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _pauseMenuUI.SetActive(true);

                _sensitivitySlider.value = _gameSettings.mouseSensitivity;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                _pauseMenuUI.SetActive(false);
            }
        }

        // Обработчик события из InputReader
        private void OnPausePerformed()
        {
            SetPaused(!_isPaused);
        }

        private void OnPlayerDead(GameObject obj)
        {
            YG2.InterstitialAdvShow();

            SetPaused(false);
            _playerDead = true;

            _enemyCountText.gameObject.SetActive(false);

            Invoke(nameof(DelayedDeath), _deathDelay);
        }

        private void DelayedDeath()
        {
            _restartButton.gameObject.SetActive(true);
            _restartButton.onClick.AddListener(async () =>
            {
                _restartButton.enabled = false;
                await _sceneLoader.LoadScene();
                _looseText.text = "Загрузка";
            });

            _looseText.gameObject.SetActive(true);
            _looseText.text = _looseMessages[Random.Range(0, _looseMessages.Length)];

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public Enemy SpawnRequest(EnemySpawnPoint enemySpawnPoint)
        {
            Enemy spawnedEnemy = _enemyFactory.Invoke(enemySpawnPoint.transform, enemySpawnPoint.enemyTypePrefab);
            spawnedEnemy.Health.OnDead += OnEnemyDead;

            _enemies.Add(spawnedEnemy);
            _enemyCountText.text = $"{_monkeysText} {_enemies.Count}";

            return spawnedEnemy;
        }

        private void OnEnemyDead(GameObject obj)
        {
            _enemies.Remove(obj.GetComponent<Enemy>());
            _enemyCountText.text = $"{_monkeysText} {_enemies.Count}";

            if (_enemies.Count <= 0)
            {
                _enemyCountText.text = $"ЗАЧИЩЕНО!";
            }
        }

        public IEnumerator MessageRoutine(string message)
        {
            _messageText.text = message;

            float timer = 0;
            while (timer < 0.5f)
            {
                _messageText.color = new Color(
                    _messageText.color.r,
                    _messageText.color.g,
                    _messageText.color.b,
                    _messageText.color.a + Time.deltaTime);
                timer += Time.deltaTime;
                yield return null;
            }
            timer = 0;

            yield return new WaitForSeconds(_messageHoldTime);

            while (timer < 0.5f)
            {
                _messageText.color = new Color(
                    _messageText.color.r,
                    _messageText.color.g,
                    _messageText.color.b,
                    _messageText.color.a - Time.deltaTime);
                timer += Time.deltaTime;
                yield return null;
            }

            _messageText.color = new Color(
                 _messageText.color.r,
                 _messageText.color.g,
                 _messageText.color.b,
                0);
        }

        public IEnumerator SetAllEnemiesOutlined()
        {
            _enemies.ForEach(e => e.SetOutlined(true));
            yield return new WaitForSeconds(_outlineTime);
            _enemies.ForEach(e => e.SetOutlined(false));
        }

        private void OnDestroy()
        {
            _playerFacade.Health.OnDead -= OnPlayerDead;
            _restartButton.onClick.RemoveAllListeners();
            _inputReader.OnPausePerformed -= OnPausePerformed;

            _disableMusicToggle.onValueChanged.RemoveAllListeners();
            _sensitivitySlider.onValueChanged.RemoveAllListeners();
        }
    }
}