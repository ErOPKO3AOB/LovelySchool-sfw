using Assets.Scripts.Weapons.Core;
using UnityEngine;

using Random = UnityEngine.Random;

namespace Assets.Scripts.Weapons.Visual
{
    public sealed class AnimationModule
    {
        public AnimationModule(WeaponContext context)
        {
            _context = context;
        }

        private readonly WeaponContext _context;

        public void PlayAttack()
        {
            Animator animator = _context.View.Animator;

            if (animator == null)
                return;

            string[] clips = _context.Config.attackAnimations;

            if (clips == null || clips.Length == 0)
                return;

            string clip = clips[Random.Range(0, clips.Length)];

            animator.CrossFade(clip, 0f, 0, 0f);
        }

        public void PlayReload()
        {
            Animator animator = _context.View.Animator;

            if (animator == null)
                return;

            animator.CrossFade(_context.Config.reloadAnimation, 0f, 0, 0f);
        }

        public void PlayPickup()
        {
            Animator animator = _context.View.Animator;

            if (animator == null)
                return;

            animator.CrossFade(_context.Config.pickupAnimation, 0f, 0, 0f);
        }
    }

}
