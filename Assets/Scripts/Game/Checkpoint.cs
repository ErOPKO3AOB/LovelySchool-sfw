using Game;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using VContainer;
using VContainer.Unity;
using PlayerPrefs = RedefineYG.PlayerPrefs;

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
        [SerializeField] private float _checkpointActivationDistance = 2f;
        [SerializeField] private float _checkpointMinTick = 0.2f;
        [SerializeField] private float _checkpointMaxTick = 0.5f;
        private float _checkpointTimer = 0;

        public bool reached;
        [HideInInspector] public bool firstCheckpoint;
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
                if (Vector3.Distance(_playerFacade.transform.position, transform.position) <= _checkpointActivationDistance)
                    ActivateCheckPoint();
            }
        }

        public void ActivateCheckPoint()
        {
            if (reached) return;

            for (int i = 0; i < _gameController.Checkpoints.Length; i++)
            {
                if (_gameController.Checkpoints[i] == this)
                {
                    reached = true;
                    _light.SetActive(false);
                    if (!firstCheckpoint)
                    {
                        PlayerPrefs.SetInt(_gameController.CHECKPOINT_PREFS_NAME, i);
                        _text.SetText("ЧЕКПОИНТ ДОСТИГНУТ!");
                        Debug.Log($"Checkpoint reached! index: {i}");
                        _text.color = Color.green;
                    }
                    break;
                }
            }
        }
    }
}