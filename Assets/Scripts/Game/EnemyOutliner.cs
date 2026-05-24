using UnityEngine;
using VContainer;

namespace Game
{
    public class EnemyOutliner : MonoBehaviour
    {
        [Inject]
        public void Construct(GameController gameController, SoundEmitterService soundEmitterService)
        {
            _gameController = gameController;
            _soundEmitterService = soundEmitterService;
        }

        private GameController _gameController;
        private SoundEmitterService _soundEmitterService;

        [SerializeField] private Animator _animator;

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