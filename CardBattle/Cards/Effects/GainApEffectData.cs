using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "GainApEffect",
        menuName = "Card Battle/Effects/Gain AP")]
    public class GainApEffectData : CardEffectData
    {
        [SerializeField] private int amount = 1;
        [SerializeField] private bool verboseLogs;

        public override string GetDescriptionText()
        {
            int value = Mathf.Max(0, amount);
            return $"Gain <color=#B0966E>{value} AP</color>";
        }

        public override void Apply(
            CardPlayContext context,
            CardEffectExecutionContext executionContext)
        {
            if (context?.Player == null)
                return;

            int requested = Mathf.Max(0, amount);
            if (requested <= 0)
                return;

            int gained = context.Player.GainAp(requested);

            if (verboseLogs)
            {
                Debug.Log(
                    $"[GainApEffect] Requested={requested} | " +
                    $"Gained={gained} | CurrentAP={context.Player.CurrentAp}");
            }
        }
    }
}
