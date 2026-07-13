using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Lightweight Play Mode harness for Gain AP effect / API validation.
    /// </summary>
    public class GainApEffectDebugTest : MonoBehaviour
    {
        private const string LogPrefix = "[GainApDebug]";

        [Header("References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private CardResolver cardResolver;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private CardData testCard;

        [Header("Test Values")]
        [SerializeField] private int testAmount = 2;
        [SerializeField] private bool verboseLogs = true;

        [ContextMenu("Debug/Print AP State")]
        public void DebugPrintApState()
        {
            if (!ValidatePlayer())
                return;

            Debug.Log(
                $"{LogPrefix}\n" +
                $"CurrentAp={player.CurrentAp}\n" +
                $"ApPerRound={player.ApPerRound}\n" +
                $"IsAlive={player.IsAlive}\n" +
                $"HasCommittedTurn={player.HasCommittedTurn}\n" +
                $"CanAct={player.CanAct}");
        }

        [ContextMenu("Debug/Gain AP Direct API")]
        public void DebugGainApDirectApi()
        {
            if (!ValidatePlayer())
                return;

            int before = player.CurrentAp;
            int requested = testAmount;
            int gained = player.GainAp(requested);
            int after = player.CurrentAp;

            Debug.Log(
                $"{LogPrefix}\n" +
                $"Before={before}\n" +
                $"Requested={requested}\n" +
                $"ActualGained={gained}\n" +
                $"After={after}\n" +
                $"ApPerRound={player.ApPerRound}");
        }

        [ContextMenu("Debug/Resolve Gain AP Test Card")]
        public void DebugResolveGainApTestCard()
        {
            if (!ValidatePlayer() || !ValidateResolver() || testCard == null)
            {
                if (testCard == null)
                    Debug.LogError($"{LogPrefix} Test CardData reference is missing.");
                return;
            }

            int before = player.CurrentAp;
            var card = new CardInstance(testCard);
            var enemies = enemyActionSystem != null ? enemyActionSystem.Enemies : null;
            var context = new CardPlayContext(player, card, enemies);
            var result = cardResolver.Resolve(context);

            Debug.Log(
                $"{LogPrefix} Resolve test card '{testCard.DisplayName}'\n" +
                $"Before={before}\n" +
                $"After={player.CurrentAp}\n" +
                $"RequestedDraw={result.RequestedDrawCount}");
        }

        [ContextMenu("Debug/Validate Gain AP Arithmetic")]
        public void DebugValidateGainApArithmetic()
        {
            if (!ValidatePlayer())
                return;

            int before = player.CurrentAp;
            int apPerRound = player.ApPerRound;
            int requested = Mathf.Max(0, testAmount);
            int gained = player.GainAp(requested);
            int after = player.CurrentAp;
            int expected = before + gained;

            bool canGain = player.IsAlive && !player.HasCommittedTurn && requested > 0;
            bool pass = after == expected &&
                        ((canGain && gained > 0 && gained <= requested) || (!canGain && gained == 0));

            if (verboseLogs)
            {
                Debug.Log(
                    $"{LogPrefix} Arithmetic\n" +
                    $"Before={before}\n" +
                    $"Requested={requested}\n" +
                    $"ActualGained={gained}\n" +
                    $"After={after}\n" +
                    $"ApPerRound={apPerRound}\n" +
                    $"AboveApPerRound={(after > apPerRound)}");
            }

            if (pass)
                Debug.Log($"{LogPrefix} PASS — Gain AP arithmetic valid");
            else
                Debug.LogError($"{LogPrefix} FAIL — Gain AP arithmetic mismatch");
        }

        private bool ValidatePlayer()
        {
            if (player != null)
                return true;

            Debug.LogError($"{LogPrefix} PlayerBattleUnit reference is missing.");
            return false;
        }

        private bool ValidateResolver()
        {
            if (cardResolver != null)
                return true;

            Debug.LogError($"{LogPrefix} CardResolver reference is missing.");
            return false;
        }
    }
}
