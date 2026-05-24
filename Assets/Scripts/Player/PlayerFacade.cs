using Assets.Scripts.Data;
using Game;
using Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Player
{
    public class PlayerFacade : MonoBehaviour
    {
        [SerializeField] private Inventory _inventory;
        [SerializeField] private Health _health;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private GameObject _ragdoll;
        [SerializeField] private Rigidbody _ragdollRb;
        [SerializeField] private GameObject _canvas;
        [SerializeField] private LegController _legController;
        [SerializeField] private Animator _v1BodyAnimator;

        private PlayerData _playerData;
        public PlayerData PlayerData { get { return _playerData; }  }
        
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

        private InputReader _input;
        private CameraFacade _camera;

        private PooledSpawnService _pooledSpawnService;

        public LegController LegController => _legController;

        public PlayerJump Jump => _jump;
        public Inventory Inventory => _inventory;
        public Health Health => _health;
        public Rigidbody RagdollRb => _ragdollRb;
        public PlayerDash Dash => _dash;
        public CameraFacade CameraFacade => _camera;

        [Inject]
        public void Construct(InputReader input, PooledSpawnService pooledSpawnService, PlayerData playerData)
        {
            _input = input;
            _pooledSpawnService = pooledSpawnService;
            _playerData = playerData;
        }

        private void OnValidate()
        {
            gameObject.layer = LayerMask.NameToLayer("Player");
        }

        private void Start()
        {
            _health.ConstructManually(_pooledSpawnService);
            _health.HealthChanged += HealthChanged;

            _health.OnDead += OnDead;


            SetupComponents();
            CreateStaminaUI();
            SpawnCamera();

            _stamina = new PlayerStamina(_playerData);
            _movement = new PlayerMovement(_rigidbody, _playerData, transform);
            _jump = new PlayerJump(_rigidbody, _playerData, _groundCheck);
            _dash = new PlayerDash(_rigidbody, _playerData, _stamina, transform);
            _inventory.Initialize(_input, _camera.Camera);

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
            _healthText.text = health.ToString();
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
            if (_playerData.cameraPrefab != null)
            {
                _camera = Instantiate(_playerData.cameraPrefab, transform.position, Quaternion.identity, transform);
                _camera.Initialize(_input, this, _playerData);
            }
        }

        private void SubscribeInput()
        {
            _input.OnJumpPerformed += HandleJump;
            _input.OnDashPerformed += HandleDash;
            _input.OnKickPerformed += () => _legController.StartKick();
        }

        private void OnEnable()
        {
            if (_input != null)
                SubscribeInput();
        }

        private void OnDisable()
        {
            _input.OnJumpPerformed -= HandleJump;
            _input.OnDashPerformed -= HandleDash;

            _stamina.OnStaminaChanged -= UpdateStaminaUI;
            _input.OnKickPerformed -= () => _legController.StartKick();
        }

        private void Update()
        {
            if (_health.IsDead) return;

            _stamina.Update(Time.deltaTime);
            _dash.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_health.IsDead) return;

            bool isGrounded = _jump.IsGrounded;

            _movement.Move(_input.Move, isGrounded, _dash.IsDashing);
            _v1BodyAnimator.SetBool("Walking", _input.Move != Vector2.zero && isGrounded);
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
            _dash.TryStartDash(_input.Move);
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

#if UNITY_EDITOR
        // Для отладки
        private void OnDrawGizmosSelected()
        {
            if (_groundCheck == null || _playerData == null) return;
            Gizmos.color = _jump != null && _jump.IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(_groundCheck.position, _playerData.groundCheckRadius);
        }
    }
#endif
}