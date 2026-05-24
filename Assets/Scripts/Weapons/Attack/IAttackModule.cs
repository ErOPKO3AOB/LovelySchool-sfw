using VContainer.Unity;

namespace Assets.Scripts.Weapons.Attack
{
    public interface IAttackModule : ITickable
    {
        void StartAttack();
        void StopAttack();
    }

}
