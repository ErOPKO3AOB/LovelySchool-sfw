using UnityEngine;
using VContainer;

namespace Game
{
    public class EnemyOutliner : MonoBehaviour
    {
        [Inject]
        public void Construct(SoundEmitterService soundEmitterService)
        {
            _soundEmitterService = soundEmitterService;
        }

        private GameController _gameController;
        private SoundEmitterService _soundEmitterService;

        [SerializeField] private Animator _animator;

        private void Start()
        {
            _gameController = FindFirstObjectByType<GameController>();
        }

        public void SetOutlined()
        {
            _soundEmitterService.PlaySound("Bell", transform, 5);
            _soundEmitterService.PlaySound("Bell", transform, 5);
            _soundEmitterService.PlaySound("Bell", transform, 5);
            _animator.Play("Bonk", 0, 0);

            _gameController.StartCoroutine(_gameController.SetAllEnemiesOutlined());
        }
    }
}