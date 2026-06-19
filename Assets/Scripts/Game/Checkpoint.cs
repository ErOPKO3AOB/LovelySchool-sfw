using Game;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using VContainer;
using VContainer.Unity;
using YG;

namespace Assets.Scripts.Game
{
    public class Checkpoint : MonoBehaviour, ITickable
    {
        [Inject]
        public void Construct(GameController gameController, PlayerFacade playerFacade)
        {
            _gameController = gameController;
            _playerFacade = playerFacade;
        }

        private GameController _gameController;
        private PlayerFacade _playerFacade;

        [Header("Visual")]
        [SerializeField] private GameObject _light;
        [SerializeField] private LookAtConstraint _lookAt;
        public TMP_Text _text;

        [Header("Other")]
        [SerializeField] private LayerMask _obstacleLayerMask;
        [SerializeField] private float _checkpointActivationDistance = 2f;
        [SerializeField] private float _checkpointMinTick = 0.2f;
        [SerializeField] private float _checkpointMaxTick = 0.5f;
        private float _checkpointTimer = 0;

        public bool reached;
        public bool firstCheckpoint;
        public bool initialized;

        private void Start()
        {
            if (firstCheckpoint)
            {
                _text.text = "";
                _light.SetActive(false);
            }

            var source = new ConstraintSource()
            {
                weight = 1,
                sourceTransform = _playerFacade.transform,
            };
            _lookAt.AddSource(source);
            _lookAt.constraintActive = true;
        }

        public void Tick()
        {
            if (reached || !initialized) return;

            _checkpointTimer -= Time.deltaTime;
            if (_checkpointTimer <= 0)
            {
                _checkpointTimer = Random.Range(_checkpointMinTick, _checkpointMaxTick);

                Vector3 playerPos = _playerFacade.transform.position;
                Vector3 checkpointPos = transform.position;
                Vector3 direction = playerPos - checkpointPos;
                float sqrDistance = direction.sqrMagnitude; // оптимизированная проверка расстояния

                if (sqrDistance <= _checkpointActivationDistance * _checkpointActivationDistance)
                {
                    // Луч от чекпоинта к игроку на точное расстояние
                    if (!Physics.Raycast(checkpointPos, direction.normalized, Mathf.Sqrt(sqrDistance), _obstacleLayerMask, QueryTriggerInteraction.Ignore))
                    {
                        ActivateCheckPoint();
                    }
                }
            }
        }

        private void ActivateCheckPoint()
        {
            if (reached) return;

            for (int i = 0; i < _gameController.Checkpoints.Length; i++)
            {
                if (_gameController.Checkpoints[i] == this)
                {
                    if (!firstCheckpoint)
                    {
                        _gameController.CurrCheckPointIndex = i;
                        _playerFacade.Inventory.SaveWeaponsState();
                        YG2.saves.currentCheckPoint = i;

                        YG2.SaveProgress();
                    }

                    DisableCheckpoint();
                    break;
                }
            }
        }

        public void DisableCheckpoint()
        {
            reached = true;
            _light.SetActive(false);

            if (!firstCheckpoint)
            {
                _text.SetText("ЧЕКПОИНТ ДОСТИГНУТ!");
                _text.color = Color.green;
            }
        }
    }
}