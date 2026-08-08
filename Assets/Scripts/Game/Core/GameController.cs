using Assets.Scripts;
using Assets.Scripts.Game;
using Assets.Scripts.Input;
using Assets.Scripts.Input.UI;
using Data;
using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using VContainer;
using YG;
using PlayerPrefs = RedefineYG.PlayerPrefs;

namespace Game
{
    public class GameController : MonoBehaviour
    {
        [Inject]
        public void Construct(
            AdvService advService,
            Func<Transform, GameObject, Enemy> enemyFactory,
            Func<Transform, EnemyOutliner> enemyOutlinerFactory,
            PlayerFacade playerFacade,
            IInputReader inputReader,
            GameSettingsSO gameSettings,
            ButtonsCanvasController buttonsCanvasController,
            CounterService сounterService,
            Func<Transform, Checkpoint> checkpointFactory
            )
        {
            _advService = advService;
            _enemyFactory = enemyFactory;
            _playerFacade = playerFacade;
            _inputReader = inputReader;
            _gameSettings = gameSettings;
            _сounterService = сounterService;
            _buttonsCanvasController = buttonsCanvasController;
            _enemyOutlinerFactory = enemyOutlinerFactory;
            _checkpointFactory = checkpointFactory;
        }

        private AdvService _advService;
        private Func<Transform, GameObject, Enemy> _enemyFactory;
        private PlayerFacade _playerFacade;
        private IInputReader _inputReader;
        private GameSettingsSO _gameSettings;
        private CounterService _сounterService;
        private ButtonsCanvasController _buttonsCanvasController;
        private Func<Transform, EnemyOutliner> _enemyOutlinerFactory;
        Func<Transform, Checkpoint> _checkpointFactory;



        public Volume globalVolume;

        [Header("Enemies")]
        [SerializeField] private List<Enemy> _enemies = new();
        [SerializeField] private TMP_Text _enemyCountText;
        [SerializeField] private float _outlineTime = 15f;
        [SerializeField] private Transform[] _enemyOutlinerTransforms;

        [Header("Rooms")]
        [SerializeField] private EnemySpawnZone[] _rooms;
        [SerializeField] private float _messageHoldTime = 6;
        [SerializeField] private List<TMP_Text> _messageTexts;
        [SerializeField] private GameObject _startEnemies;
        public SceneLoader _winningSceneLoader;

        [Header("Death")]
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _skipCheckpointButton;
        [SerializeField] private TMP_Text _skipText;
        [SerializeField] private TMP_Text _errorSkipText;
        [SerializeField] private TMP_Text _looseText;
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private float _deathDelay = 2f;

        [Header("Pause")]
        [SerializeField] private GameObject _pauseMenuUI;
        [SerializeField] private Toggle _disableMusicToggle;
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private Toggle _disableHintsToggle;

        [Header("CheckPoints")]
        [SerializeField] private Transform[] _checkPointPositions;

        public UnityAction<bool> OnPauseStateChanged;

        private bool _isPaused = false;

        private bool _playerDead = false;
        public EnemySpawnZone[] Rooms => _rooms;

        public bool IsPaused => _isPaused;
        public Toggle DisableMusicToggle => _disableMusicToggle;

        public int CurrCheckPointIndex { get; set; }
        public Checkpoint[] Checkpoints { get; private set; }

        public PlayerFacade PlayerFacade => _playerFacade;

        // GameController.cs

        private void Awake()
        {
            CurrCheckPointIndex = YG2.saves.currentCheckPoint;
            CurrCheckPointIndex = Mathf.Clamp(CurrCheckPointIndex, 0, _rooms.Length - 1);
            Debug.Log("Checkpoint index: " + CurrCheckPointIndex);

            for (int i = 0; i < _rooms.Length; i++)
            {
                if (_rooms[i] != null)
                {
                    _rooms[i].SetRoomIndex(i, this);   // сначала запоминаем индекс и флаг
                    _rooms[i].Initialize();            // только потом инициализируем
                }
            }

            _winningSceneLoader.OnSceneStartedLoading += () => _сounterService.SendMetrics();
        }

        private void Start()
        {
            _sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", _gameSettings.mouseSensitivity);
            _disableMusicToggle.isOn = PlayerPrefs.GetInt("MusicOff", 0) == 1;
            _disableHintsToggle.isOn = PlayerPrefs.GetInt("HintsOff", 0) == 1;
            _buttonsCanvasController.SetHints(!_disableHintsToggle.isOn);

            _gameSettings.mouseSensitivity = _sensitivitySlider.value;

            YG2.SwitchLanguage(PlayerPrefs.GetString("Language", YG2.lang));

            // Checkpoints
            if (_checkPointPositions != null && _checkPointPositions.Length > 0)
            {
                Checkpoints = new Checkpoint[_checkPointPositions.Length];
                for (int i = 0; i < _checkPointPositions.Length; i++)
                {
                    Checkpoints[i] = _checkpointFactory.Invoke(_checkPointPositions[i]);
                    if (i == 0)
                    {
                        Checkpoints[i].firstCheckpoint = true;
                    }

                    else if (i == _checkPointPositions.Length - 1)
                    {
                        Checkpoints[i].last = true;
                    }
                }
            }

            TryLoadCheckpoint();

            // Other
            if (!YG2.envir.isDesktop) _disableHintsToggle.gameObject.SetActive(false);
            else
            {
                _disableHintsToggle.onValueChanged.AddListener((value) =>
                {
                    ActivateHintTexts(!value);
                });
            }

            for (int i = 0; i < _rooms.Length; i++)
            {
                _rooms[i].OnCleaned += (index, roomName) =>
                {
                    StartCoroutine(MessageRoutine(index));
                    _сounterService.SetLastCompletedRoom(roomName);
                };
            }

            for (int i = 0; i < _enemyOutlinerTransforms.Length; i++)
            {
                _enemyOutlinerFactory.Invoke(_enemyOutlinerTransforms[i]);
            }

            // Player
            _playerFacade.Health.OnDead += OnPlayerDead;

            _inputReader.OnPausePerformed += OnPausePerformed;

            _pauseMenuUI.SetActive(true);

            _sensitivitySlider.onValueChanged.AddListener((value) =>
            {
                _gameSettings.mouseSensitivity = value;
            });

            _pauseMenuUI.SetActive(false);
        }

        private void Update()
        {
            foreach (var checkPoints in Checkpoints)
            {
                if (checkPoints.gameObject.activeSelf)
                    checkPoints.Tick();
            }
        }

        public void SetPaused(bool paused)
        {
            if (_playerDead) return;

            _isPaused = paused;
            YG2.PauseGame(paused);

            OnPauseStateChanged?.Invoke(paused);

            if (paused)
            {
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _pauseMenuUI.SetActive(true);
                YG2.GameplayStop();
            }
            else
            {
                PlayerPrefs.SetInt("HintsOff", _disableHintsToggle.isOn ? 1 : 0);
                PlayerPrefs.SetInt("MusicOff", _disableMusicToggle.isOn ? 1 : 0);
                PlayerPrefs.SetFloat("Sensitivity", _sensitivitySlider.value);

                Time.timeScale = 1f;
                if (YG2.envir.device == YG2.Device.Desktop)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                _pauseMenuUI.SetActive(false);
                YG2.GameplayStart();
                //GameReadyAPI.SafeGameplayStart().Forget();
            }
        }

        private void ActivateHintTexts(bool value)
        {
            if (!YG2.envir.isDesktop) return;

            _buttonsCanvasController.SetHints(value);
        }

        // Обработчик события из InputReader
        private void OnPausePerformed()
        {
            SetPaused(!_isPaused);
        }

        private void OnPlayerDead(GameObject obj)
        {
            Destroy(_buttonsCanvasController.gameObject);

            _playerDead = true;

            _enemyCountText.gameObject.SetActive(false);

            Invoke(nameof(DelayedDeath), _deathDelay);
            YG2.GameplayStop();
        }

        private void DelayedDeath()
        {
            _restartButton.gameObject.SetActive(true);
            _restartButton.onClick.AddListener(RestartGame);

            if (CurrCheckPointIndex < Checkpoints.Length - 1)
            {
                _skipCheckpointButton.gameObject.SetActive(true);
                _skipCheckpointButton.onClick.AddListener(() =>
                {
                    _skipCheckpointButton.interactable = false;

                    YG2.onErrorRewardedAdv += AdError;
                    YG2.onCloseRewardedAdv += AdClose;

                    void AdError()
                    {
                        _skipText.gameObject.SetActive(false);
                        _errorSkipText.gameObject.SetActive(true);
                        _skipCheckpointButton.interactable = true;
                        YG2.onRewardAdv -= SkipCheckpointReward;
                        YG2.onErrorRewardedAdv -= AdError;
                    }

                    void AdClose()
                    {
                        _skipCheckpointButton.gameObject.SetActive(false);
                        YG2.onCloseRewardedAdv -= AdClose;
                    }

                    YG2.onRewardAdv += SkipCheckpointReward;
                    YG2.RewardedAdvShow("skip-checkpoint");
                });
            }

            _looseText.gameObject.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestartGame()
        {
            _restartButton.enabled = false;


            void LoadScene()
            {
                YG2.onCloseAnyAdv -= LoadScene;
                YG2.onErrorAnyAdv -= LoadScene;

                _sceneLoader.LoadScene();
            }

            if (!YG2.isTimerAdvCompleted) LoadScene();
            else
            {
                YG2.onCloseAnyAdv += LoadScene;
                YG2.onErrorAnyAdv += LoadScene;

                _advService.ShowInterstitial();
            }
        }

        private void SkipCheckpointReward(string id)
        {
            if (id == "skip-checkpoint")
            {
                _skipCheckpointButton.gameObject.SetActive(false);

                YG2.saves.currentCheckPoint++;
                YG2.onRewardAdv -= SkipCheckpointReward;

                RestartGame();
            }
        }

        public Enemy SpawnRequest(EnemySpawnPoint enemySpawnPoint)
        {
            Enemy spawnedEnemy = _enemyFactory.Invoke(enemySpawnPoint.transform, enemySpawnPoint.enemyTypePrefab);
            spawnedEnemy.Health.OnDead += OnEnemyDead;

            _enemies.Add(spawnedEnemy);
            _enemyCountText.text = _enemies.Count.ToString();

            return spawnedEnemy;
        }

        private void OnEnemyDead(GameObject obj)
        {
            if (obj.TryGetComponent<Enemy>(out var enemy))
            {
                _enemies.Remove(enemy);
                UpdateEnemyCounter();
            }
        }

        public void RemoveEnemyFromList(Enemy enemy)
        {
            if (_enemies.Remove(enemy))
                UpdateEnemyCounter();
        }

        private void UpdateEnemyCounter()
        {
            if (_enemies.Count <= 0)
                _enemyCountText.text = "0";
            else
                _enemyCountText.text = _enemies.Count.ToString();
        }

        public IEnumerator MessageRoutine(int index)
        {
            _messageTexts[index].gameObject.SetActive(true);

            // Сохраняем исходный цвет (без альфы)
            Color baseColor = _messageTexts[index].color;
            float duration = 0.5f;
            float timer = 0f;

            // --- Появление (0 → 1) ---
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;          // от 0 до 1
                float alpha = Mathf.Lerp(0f, 1f, progress); // плавно от 0 к 1
                _messageTexts[index].color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
            _messageTexts[index].color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f); // фиксируем 1

            // --- Пауза ---
            yield return new WaitForSeconds(_messageHoldTime);

            // --- Исчезновение (1 → 0) ---
            timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = timer / duration;
                float alpha = Mathf.Lerp(1f, 0f, progress); // плавно от 1 к 0
                _messageTexts[index].color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
            _messageTexts[index].color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f); // гарантируем 0

            _messageTexts[index].gameObject.SetActive(false);
        }

        public IEnumerator SetAllEnemiesOutlined()
        {
            _enemies.ForEach(e => e.SetOutlined(true));
            yield return new WaitForSeconds(_outlineTime);
            _enemies.ForEach(e => e.SetOutlined(false));
        }

        private void TryLoadCheckpoint()
        {
            if (CurrCheckPointIndex == 0)
            {
                _startEnemies.SetActive(true);
            }
            else
            {
                // Активируем чекпоинты (визуал и сохранение)
                for (int i = 0; i < CurrCheckPointIndex; i++)
                {
                    Checkpoints[i].DisableCheckpoint();
                    // DeactivateForCheckpoint для этих комнат уже вызван в их Start
                }
            }

            var chkpntTransform = Checkpoints[CurrCheckPointIndex].transform;
            _playerFacade.Teleport(chkpntTransform.position, chkpntTransform.rotation);
            //GameReadyAPI.SafeGameplayStart().Forget();
            YG2.GameplayStart();
            Debug.Log("Loaded checkpoint");

            foreach (var checkpoints in Checkpoints)
                checkpoints.initialized = true;

            for (int i = CurrCheckPointIndex + 1; i < Checkpoints.Length; i++)
            {
                Checkpoints[i].gameObject.SetActive(false);
            }
        }

        public List<Enemy> GetEnemiesOfZone(EnemySpawnZone zone)
        {
            List<Enemy> result = new List<Enemy>();
            foreach (var enemy in _enemies)
            {
                if (enemy != null && enemy.SpawnZone == zone)
                    result.Add(enemy);
            }
            return result;
        }

        private void OnDestroy()
        {
            _playerFacade.Health.OnDead -= OnPlayerDead;
            _restartButton.onClick.RemoveAllListeners();
            _inputReader.OnPausePerformed -= OnPausePerformed;

            _disableMusicToggle.onValueChanged.RemoveAllListeners();
            _sensitivitySlider.onValueChanged.RemoveAllListeners();
            _disableHintsToggle.onValueChanged.RemoveAllListeners();

            _skipCheckpointButton.onClick.RemoveAllListeners();
            YG2.onRewardAdv -= SkipCheckpointReward;
        }
    }
}