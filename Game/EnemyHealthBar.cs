using Assets.Scripts.Game;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

namespace Game
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Slider _slider;
        [SerializeField] private LookAtConstraint _lookAt;
        [SerializeField] private Enemy _enemy;

        private void Start()
        {
            _slider.maxValue = _health.MaxHealth;
            _slider.minValue = 0;
            _slider.value = _health.CurrentHealth;

            _health.HealthChanged += HealthChanged;

            var source = new ConstraintSource()
            {
                weight = 1,
                sourceTransform = _enemy.PlayerFacade.transform,
            };
            _lookAt.AddSource(source);
        }

        private void HealthChanged(int obj)
        {
            _slider.value = obj;
        }

        private void OnDestroy()
        {
            _health.HealthChanged -= HealthChanged;
        }
    }
}