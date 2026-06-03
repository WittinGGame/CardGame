using System;
using UnityEngine;

namespace CardBattle.Core
{
    public class BattleUnitView : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DefenseHash = Animator.StringToHash("Defense");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        public event Action OnAttackHit;
        public event Action OnAttackPreHit;
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

        public void PlayDefense()
        {
            if (animator == null) return;
            animator.SetTrigger(DefenseHash);
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
        
        public void AnimEvent_AttackPreHit()
        {
            OnAttackPreHit?.Invoke();
        }

        // Animation Event
        public void AnimEvent_ActionFinished()
        {
            OnActionFinished?.Invoke();
        }
    }
}