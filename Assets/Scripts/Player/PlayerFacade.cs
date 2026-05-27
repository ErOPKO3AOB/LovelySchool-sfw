using Assets.Scripts.Input;
using Data;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using VContainer;

namespace Player
{
    public class PlayerFacade : MonoBehaviour
    {
        [SerializeField] private GameSettingsSO _settings;
        [SerializeField] private Inventory _inventory;
        [SerializeField] private Health _health;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private GameObject _ragdoll;
        [SerializeField] private Rigidbody _ragdollRb;
        [SerializeField] private GameObject _canvas;
        [SerializeField] private LegController _legController;
        [SerializeField] private Animator _v1BodyAnimator;

        [Header("Effects")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField, Range(0, 1)] private float _hpPercent = 1f;
        //[SerializeField] private int _lowHpAudioHerz;
        //[SerializeField] private int _defaultAudioHerz;
        //private AudioLowPassFilter _audioLowPassFilter;
        private float _currentCutoff;
        private float _cutoffVelocity;

        // Компоненты
        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;
        private Transform _groundCheck;

        // Модули
        private PlayerStamina _stamina;
        private PlayerMovement _movement;
        private PlayerJump _jump;
        private PlayerDash _dash;

        // УЖЕ ЕСТЬ ДОЧЕРНИЙ ОБЪЕКТ В ИГРОКЕ 
        [SerializeField] private Slider _staminaSlider;

        private IInputReader _inputReader;
        private CameraFacade _camera;

        private PooledSpawnService _pooledSpawnService;
        public LegController LegController => _legController;

        public PlayerJump Jump => _jump;
        public Inventory Inventory => _inventory;
        public Health Health => _health;
        public Rigidbody RagdollRb => _ragdollRb;
        public PlayerDash Dash => _dash;

        public GameSettingsSO GameSettings { get; private set; }
        private GameController _gameController;
        private Vignette _vignette;
        private float _vignetteVelocity;
        private ColorAdjustments _colorAdjustments;
        private float _colorAdjustmentsVelocity;

        [Inject]
        public void Construct(IInputReader inputReader, PooledSpawnService pooledSpawnService, GameSettingsSO gameSettings)
        {
            _inputReader = inputReader;
            _pooledSpawnService = pooledSpawnService;
            GameSettings = gameSettings;
        }

        private void OnValidate()
        {
            gameObject.layer = LayerMask.NameToLayer("Player");
        }

        private void Start()
        {
            _gameController = FindFirstObjectByType<GameController>();
            _gameController.globalVolume.profile.TryGet(out _vignette);
            _gameController.globalVolume.profile.TryGet(out _colorAdjustments);

            _health.ConstructManually(_pooledSpawnService);
            _health.HealthChanged += HealthChanged;

            _health.OnDead += OnDead;


            SetupComponents();
            CreateStaminaUI();
            SpawnCamera();

            _stamina = new PlayerStamina(_settings);
            _movement = new PlayerMovement(_rigidbody, _settings, transform); // больше не нужен _cameraTransform
            _jump = new PlayerJump(_rigidbody, _settings, _groundCheck);
            _dash = new PlayerDash(_rigidbody, _settings, _stamina, transform);
            _inventory.Initialize(_inputReader, _camera.Camera);

            _stamina.OnStaminaChanged += UpdateStaminaUI;

            SubscribeInput();
        }

        private void OnDead(GameObject obj)
        {
            _rigidbody.isKinematic = true;
            _collider.enabled = false;
            _ragdoll.SetActive(true);
            _canvas.SetActive(false);
            _v1BodyAnimator.gameObject.SetActive(false);

            _ragdoll.transform.SetParent(null);

            Rigidbody[] ragdollRbs = _ragdoll.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in ragdollRbs)
            {
                // Случайное направление
                Vector3 randomDir = Random.onUnitSphere;
                // Случайная сила (можно настроить диапазон)
                float force = Random.Range(5f, 20f);

                rb.AddForce(randomDir * force, ForceMode.VelocityChange);

                // Добавляем случайный вращательный момент
                Vector3 randomTorque = Random.insideUnitSphere * Random.Range(1f, 5f);
                rb.AddTorque(randomTorque, ForceMode.VelocityChange);
            }
        }

        private void HealthChanged(int health)
        {
            _healthText.text = health.ToString() + "хп";
        }

        private void SetupComponents()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.freezeRotation = true;

            _collider = GetComponent<CapsuleCollider>();

            // Создаём GroundCheck, если его нет
            if (_groundCheck == null)
            {
                var go = new GameObject("GroundCheck");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0, -_collider.height * 0.4f, 0);
                _groundCheck = go.transform;
            }
        }

        private void CreateStaminaUI()
        {
            _staminaSlider.interactable = false;
            _staminaSlider.transition = Selectable.Transition.None;

            _staminaSlider.value = 1f;
        }

        private void UpdateStaminaUI(float current, float max)
        {
            if (_staminaSlider != null)
                _staminaSlider.value = current / max;
        }

        private void SpawnCamera()
        {
            if (_settings.cameraPrefab != null)
            {
                _camera = Instantiate(_settings.cameraPrefab, transform.position, Quaternion.identity, transform);
                _camera.Initialize(_inputReader, this, _settings);
            }
        }

        private void SubscribeInput()
        {
            _inputReader.OnJumpPerformed += HandleJump;
            _inputReader.OnDashPerformed += HandleDash;
            _inputReader.OnKickPerformed += () => _legController.StartKick();
        }

        private void OnEnable()
        {
            if (_inputReader != null)
                SubscribeInput();
        }

        private void OnDisable()
        {
            _inputReader.OnJumpPerformed -= HandleJump;
            _inputReader.OnDashPerformed -= HandleDash;

            _stamina.OnStaminaChanged -= UpdateStaminaUI;
            _inputReader.OnKickPerformed -= () => _legController.StartKick();
        }

        private void Update()
        {
            UpdateLowHpVisuals();

            if (_health.IsDead) return;

            _stamina.Update(Time.deltaTime);
            _dash.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_health.IsDead) return;

            bool isGrounded = _jump.IsGrounded;

            _movement.Move(_inputReader.Move, isGrounded, _dash.IsDashing);
            _v1BodyAnimator.SetBool("Walking", _inputReader.Move != Vector2.zero && isGrounded);
            _v1BodyAnimator.SetBool("Falling", !isGrounded);
        }

        private void HandleJump()
        {
            if (_health.IsDead) return;
            _jump.Jump();
            // Оповестить камеру о прыжке (если нужны эффекты)
        }

        private void HandleDash()
        {
            if (_health.IsDead) return;
            _dash.TryStartDash(_inputReader.Move);
        }

        // Добавьте в класс PlayerFacade

        public void RotateHorizontally(float angleDelta)
        {
            if (_health.IsDead) return;
            if (angleDelta == 0f || _rigidbody == null) return;

            // Плавный поворот через физику (MoveRotation)
            Quaternion deltaRot = Quaternion.Euler(0f, angleDelta, 0f);
            Quaternion targetRot = _rigidbody.rotation * deltaRot;
            _rigidbody.MoveRotation(targetRot);
        }

        private void OnDestroy()
        {
            _health.HealthChanged -= HealthChanged;

            _health.OnDead -= OnDead;
        }

        private void UpdateLowHpVisuals()
        {
            float time = Time.deltaTime * 20;
            float soundTarget = 1;
            float colorTarget = 0;
            float vigneteTarget = 0.2f;
            if (!_health.IsDead)
            {
                bool lowHp = (float)_health.CurrentHealth / 100 <= _hpPercent;
                soundTarget = lowHp ? 0.45f : soundTarget;
                colorTarget = lowHp ? -65f : colorTarget;
                vigneteTarget = lowHp ? 0.4f : vigneteTarget;
            }

            _currentCutoff = Mathf.SmoothDamp(_currentCutoff, soundTarget, ref _cutoffVelocity, time);
            AudioListener.volume = _currentCutoff;
            //_audioMixer.SetFloat("LowHpFilter", _currentCutoff);

            _colorAdjustments.saturation.value = Mathf.SmoothDamp(_colorAdjustments.saturation.value, colorTarget, ref _colorAdjustmentsVelocity, time);
            _vignette.intensity.value = Mathf.SmoothDamp(_vignette.intensity.value, vigneteTarget, ref _vignetteVelocity, time);
        }

#if UNITY_EDITOR
        // Для отладки
        private void OnDrawGizmosSelected()
        {
            if (_groundCheck == null || _settings == null) return;
            Gizmos.color = _jump != null && _jump.IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(_groundCheck.position, _settings.groundCheckRadius);
        }
#endif
    }
}