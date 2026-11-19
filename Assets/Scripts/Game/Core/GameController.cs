using Assets.Scripts.Input;
using Assets.Scripts.Input.UI;
using Data;
using Game;
using Player;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UniWebKit;
using VContainer;

namespace Assets.Scripts.Game.Core
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
        [SerializeField] private Image _messageBackground;
        [SerializeField] private GameObject _startEnemies;
        public SceneLoader _winningSceneLoader;
        [SerializeField] private ExitKeyPickup _exitKeyPrefab;
        private bool _exitKeyDropped;

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
        [SerializeField] private SahurSoundPlayer _sahurSoundPlayer;

        // GameController.cs

        private bool _initialized;
        private bool _startupPause;
        private float _startupTimeScale;

        private const int DeathsRequiredForSkip = 3;
        // Session-only state survives scene reloads, but is never written to player saves.
        private static int _deathsSinceLastSkip;

        private bool CanSkipCheckpoint =>
            _deathsSinceLastSkip >= DeathsRequiredForSkip &&
            Checkpoints != null &&
            CurrCheckPointIndex >= 0 && CurrCheckPointIndex < Checkpoints.Length - 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSkipSession() => _deathsSinceLastSkip = 0;

        private void Awake()
        {
            _skipCheckpointButton.gameObject.SetActive(false);
            _startupTimeScale = Time.timeScale;
            _startupPause = true;
            _isPaused = true;
            Time.timeScale = 0f;
        }

        private IEnumerator Start()
        {
            yield return GameSaveSession.Instance.EnsureLoaded();
            // Player and spawner Start methods can run in either order, even during a synchronous local load.
            while (!_playerFacade.Inventory.IsRestored) yield return null;
            UniWebBridge.Player.TryGet("currentCheckPoint", out int currentCheckPoint);
            CurrCheckPointIndex = currentCheckPoint;
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

            _sensitivitySlider.value = UniWebBridge.Player.Get("Sensitivity",
                UniWebBridge.Envir.IsDesktop ?
                _sensitivitySlider.maxValue / 5 :
                _sensitivitySlider.maxValue / 2
            );

            _disableMusicToggle.isOn = UniWebBridge.Player.Get("MusicOff", 0) == 1;
            _disableHintsToggle.isOn = UniWebBridge.Player.Get("HintsOff", 0) == 1;
            _buttonsCanvasController.SetHints(!_disableHintsToggle.isOn);

            _gameSettings.mouseSensitivity = _sensitivitySlider.value;

            var language = PlayerPrefs.GetString("Language", "");
            if (string.IsNullOrWhiteSpace(language)) language = UniWebBridge.Player.Get("Language", UniWebBridge.Localization.Language);
            UniWebBridge.Localization.SetLanguage(language);

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
            //if (!YG2.envir.isDesktop) _disableHintsToggle.gameObject.SetActive(false);
            if (!UniWebBridge.Envir.IsDesktop) _disableHintsToggle.gameObject.SetActive(false);
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
                    _sahurSoundPlayer.TryPlaySahur();
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
            _initialized = true;
            _isPaused = false;
            _startupPause = false;
            Time.timeScale = _startupTimeScale;
        }

        private void Update()
        {
            if (!_initialized || Checkpoints == null) return;
            foreach (var checkPoints in Checkpoints)
            {
                if (checkPoints.gameObject.activeSelf)
                    checkPoints.Tick();
            }
        }

        public void SetPaused(bool paused)
        {
            if (!_initialized || _playerDead) return;

            _isPaused = paused;
            //YG2.PauseGame(paused);
            UniWebBridge.Pause.SetPaused(paused);

            OnPauseStateChanged?.Invoke(paused);

            if (paused)
            {
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _pauseMenuUI.SetActive(true);
                //YG2.GameplayStop();
                UniWebBridge.Game.GameplayStop();
            }
            else
            {
                GameSaveSession.Set("HintsOff", _disableHintsToggle.isOn ? 1 : 0);
                GameSaveSession.Set("MusicOff", _disableMusicToggle.isOn ? 1 : 0);
                GameSaveSession.Set("Sensitivity", _sensitivitySlider.value);
                GameSaveSession.Set("Language", TranslationDropdownController.CurrentLanguage);
                GameSaveSession.Save();

                Time.timeScale = 1f;
                if (UniWebBridge.Envir.IsDesktop)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }

                _pauseMenuUI.SetActive(false);
                UniWebBridge.Game.GameplayStart();
                //GameReadyAPI.SafeGameplayStart().Forget();
            }
        }

        private void ActivateHintTexts(bool value)
        {
            if (!UniWebBridge.Envir.IsDesktop) return;

            _buttonsCanvasController.SetHints(value);
        }

        // Обработчик события из InputReader
        private void OnPausePerformed()
        {
            SetPaused(!_isPaused);
        }

        private void OnPlayerDead(GameObject obj)
        {
            if (_playerDead) return;

            Destroy(_buttonsCanvasController.gameObject);

            _playerDead = true;
            _deathsSinceLastSkip = Mathf.Min(_deathsSinceLastSkip + 1, DeathsRequiredForSkip);

            _enemyCountText.gameObject.SetActive(false);

            Invoke(nameof(DelayedDeath), _deathDelay);
            UniWebBridge.Game.GameplayStop();
        }

        private void DelayedDeath()
        {
            _restartButton.gameObject.SetActive(true);
            _restartButton.onClick.AddListener(RestartGame);
            _restartButton.interactable = true;

            _skipCheckpointButton.gameObject.SetActive(CanSkipCheckpoint);
            if (CanSkipCheckpoint)
            {
                _skipCheckpointButton.onClick.AddListener(() =>
                {
                    if (!CanSkipCheckpoint) return;

                    _restartButton.interactable = false;
                    _skipCheckpointButton.interactable = false;


                    void AdError(string error)
                    {
                        _skipText.gameObject.SetActive(false);
                        _errorSkipText.gameObject.SetActive(true);
                        _skipCheckpointButton.interactable = true;

                        _restartButton.interactable = true;
                    }

                    void AdClose(bool closed)
                    {
                        _skipCheckpointButton.gameObject.SetActive(CanSkipCheckpoint);
                        _skipCheckpointButton.interactable = true;

                        _restartButton.interactable = true;
                    }

                    _advService.ShowRewarded(onReward: SkipCheckpointReward, onClosed: AdClose, onError: AdError, placementId: "skip-checkpoint");
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
                _sceneLoader.LoadScene();
            }

            GameSaveSession.Save();
            if (!UniWebBridge.Ads.IsInterstitialReady) LoadScene();
            else UniWebBridge.Ads.ShowInterstitial(onClosed: _ => LoadScene());
        }

        private void SkipCheckpointReward()
        {
            if (!CanSkipCheckpoint) return;

            if (!GameSaveSession.Set("currentCheckPoint", CurrCheckPointIndex + 1)) return;
            _deathsSinceLastSkip = 0;
            _skipCheckpointButton.gameObject.SetActive(false);
            GameSaveSession.Save();
            RestartGame();
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
            if (!obj.TryGetComponent<Enemy>(out var enemy) || !_enemies.Remove(enemy)) return;

            UpdateEnemyCounter();
            if (_enemies.Count == 0 && !_exitKeyDropped)
                StartCoroutine(DropExitKeyAfterRoomClear(obj.transform.position));
        }

        private IEnumerator DropExitKeyAfterRoomClear(Vector3 enemyPosition)
        {
            // Последний враг комнаты открывает выход в своём обработчике смерти.
            yield return null;

            if (_exitKeyDropped || _enemies.Count != 0 || !_winningSceneLoader.isActiveAndEnabled ||
                FindAnyObjectByType<EnemySpawnPoint>() != null)
                yield break;

            Instantiate(_exitKeyPrefab, enemyPosition + Vector3.up * 0.5f, _exitKeyPrefab.transform.rotation);
            _exitKeyDropped = true;
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
            // Funny feature: if the player has already found the machine gun, the message will be different.
            if (index == 2)
            {
                if (PlayerFacade.Inventory.Weapons.Find(w => w.weaponName == "MachineGun") != null)
                {
                    if (UniWebBridge.Localization.Language == "ru")
                        _messageTexts[index].text = "А ты уже нашел пулемет, паркурист..";
                    else
                        _messageTexts[index].text = "So, have you found the machine gun yet, parkourist...";
                }
                else
                {
                    if (UniWebBridge.Localization.Language == "ru")
                        _messageTexts[index].text = "ИЩИ ПУЛЕМЕТ НА ВЫСТУПЕ!";
                    else
                        _messageTexts[index].text = "Find the machine gun on the ledge!";
                }
            }

            _messageTexts[index].gameObject.SetActive(true);

            Color textColor = _messageTexts[index].color;
            Color bgColor = _messageBackground.color;

            float duration = 0.5f;
            float timer = 0f;

            // Начальная прозрачность
            textColor.a = 0f;
            bgColor.a = 0f;
            const float backgroundAlpha = 0.6f;

            _messageTexts[index].color = textColor;
            _messageBackground.color = bgColor;

            // --- Появление ---
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Clamp01(timer / duration);

                textColor.a = alpha;
                bgColor.a = alpha * backgroundAlpha;

                _messageTexts[index].color = textColor;
                _messageBackground.color = bgColor;

                yield return null;
            }

            textColor.a = 1f;
            bgColor.a = backgroundAlpha;

            _messageTexts[index].color = textColor;
            _messageBackground.color = bgColor;

            // --- Пауза ---
            yield return new WaitForSeconds(_messageHoldTime);

            // --- Исчезновение ---
            timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(timer / duration);

                textColor.a = alpha;
                bgColor.a = alpha * backgroundAlpha;

                _messageTexts[index].color = textColor;
                _messageBackground.color = bgColor;

                yield return null;
            }

            textColor.a = 0f;
            bgColor.a = 0f;

            _messageTexts[index].color = textColor;
            _messageBackground.color = bgColor;

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
            UniWebBridge.Game.GameplayStart();
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
            if (_startupPause) Time.timeScale = _startupTimeScale;
            if (!_initialized) return;
            _playerFacade.Health.OnDead -= OnPlayerDead;
            _restartButton.onClick.RemoveAllListeners();
            _inputReader.OnPausePerformed -= OnPausePerformed;

            _disableMusicToggle.onValueChanged.RemoveAllListeners();
            _sensitivitySlider.onValueChanged.RemoveAllListeners();
            _disableHintsToggle.onValueChanged.RemoveAllListeners();

            _skipCheckpointButton.onClick.RemoveAllListeners();
        }
    }
}
