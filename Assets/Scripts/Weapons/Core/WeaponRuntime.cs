using Assets.Scripts.Weapons.Ammo;
using Assets.Scripts.Weapons.Attack;
using VContainer.Unity;

namespace Assets.Scripts.Weapons.Core
{
    public sealed class WeaponRuntime : ITickable
    {
        private readonly IAttackModule _attack;
        private readonly ReloadModule _reload;

        public WeaponRuntime(
            IAttackModule attack,
            ReloadModule reload)
        {
            _attack = attack;
            _reload = reload;
        }

        public void StartAttack()
        {
            _attack.StartAttack();
        }

        public void StopAttack()
        {
            _attack.StopAttack();
        }

        public void Reload()
        {
            _reload.StartReload();
        }

        public void Tick()
        {
            _attack.Tick();
            _reload.Tick();
        }
    }

}
