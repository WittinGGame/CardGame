using System;
using UnityEngine;

namespace CardBattle.Core
{
    public class BattleUnitView : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        public event Action OnAttackHit;
        public event Action OnActionFinished;

        public void PlayAttack()
        {
            if (animator == null) return;
            animator.SetTrigger(AttackHash);
        }

        public void PlayHurt()
        {
            if (animator == null) return;
            animator.SetTrigger(HurtHash);
        }

        public void PlayDead()
        {
            if (animator == null) return;
            animator.SetTrigger(DeadHash);
        }

        // Animation Event
        public void AnimEvent_AttackHit()
        {
            OnAttackHit?.Invoke();
        }

        // Animation Event
        public void AnimEvent_ActionFinished()
        {
            OnActionFinished?.Invoke();
        }
    }
}