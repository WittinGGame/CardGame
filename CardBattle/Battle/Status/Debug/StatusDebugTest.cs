using UnityEngine;

namespace CardBattle.Core
{
    public class StatusDebugTest : MonoBehaviour
    {
        [SerializeField] private BattleUnit attacker;
        [SerializeField] private BattleUnit defender;
        [SerializeField] private int testDamage = 10;

        [ContextMenu("Add Strength +3 to Attacker")]
        private void AddStrengthToAttacker()
        {
            attacker?.ApplyStatus(StatusEffectType.Strength, 3, StatusDurationType.Encounter, 0);
        }

        [ContextMenu("Add Weak 2 Turns to Attacker")]
        private void AddWeakToAttacker()
        {
            attacker?.ApplyStatus(StatusEffectType.Weak, 1, StatusDurationType.Turn, 2);
        }

        [ContextMenu("Add NextAttackBonus +5 to Attacker")]
        private void AddNextAttackBonusToAttacker()
        {
            attacker?.ApplyStatus(StatusEffectType.NextAttackBonus, 5, StatusDurationType.UseCount, 1);
        }

        [ContextMenu("Add Vulnerable 2 Turns to Defender")]
        private void AddVulnerableToDefender()
        {
            defender?.ApplyStatus(StatusEffectType.Vulnerable, 1, StatusDurationType.Turn, 2);
        }

        [ContextMenu("Deal Test Attack Damage")]
        private void DealTestAttackDamage()
        {
            if (defender == null)
                return;

            int finalDamage = defender.TakeAttackDamage(attacker, testDamage);
            Debug.Log($"StatusDebugTest: base={testDamage}, final HP damage={finalDamage}");
        }

        [ContextMenu("Tick Turn Duration")]
        private void TickTurnDuration()
        {
            attacker?.TickStatusTurnDuration();
            defender?.TickStatusTurnDuration();
        }

        [ContextMenu("Clear All")]
        private void ClearAll()
        {
            attacker?.ClearStatuses();
            defender?.ClearStatuses();
        }

        [ContextMenu("Print")]
        private void Print()
        {
            string attackerText = attacker?.StatusController != null
                ? attacker.StatusController.BuildDebugText()
                : "(no attacker)";

            string defenderText = defender?.StatusController != null
                ? defender.StatusController.BuildDebugText()
                : "(no defender)";

            Debug.Log($"StatusDebugTest Attacker: {attackerText}");
            Debug.Log($"StatusDebugTest Defender: {defenderText}");
        }
    }
}
