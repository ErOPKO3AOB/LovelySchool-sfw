using UnityEngine;

namespace Assets.Scripts.Weapons
{
    public class Melee : WeaponModel
    {
        public override void Tick()
        {
            base.Tick();

            Debug.Log("Don`t make me vomit");
        }

        private void PerformMelee()
        {
            view.PlayAttackAnimation();
            if (view.Animator == null)
                ShootMelee();
        }

        public void ShootMelee()
        {
            //PhysicCast();
        }
    }
}