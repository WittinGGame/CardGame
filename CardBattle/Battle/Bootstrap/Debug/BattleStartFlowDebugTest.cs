using UnityEngine;

namespace CardBattle.Core
{
    public class BattleStartFlowDebugTest : MonoBehaviour
    {
        [SerializeField] private BattleTestBootstrap battleTestBootstrap;

        [ContextMenu("Debug Start Battle")]
        private void DebugStartBattle()
        {
            if (!TryGetBootstrap())
                return;

            battleTestBootstrap.StartTestBattle();
            DebugPrint();
        }

        [ContextMenu("Debug Print Battle Start Flow")]
        private void DebugPrint()
        {
            if (!TryGetBootstrap())
                return;

            battleTestBootstrap.DebugPrintBattleStartFlowState();
        }

        private bool TryGetBootstrap()
        {
            if (battleTestBootstrap != null)
                return true;

            Debug.LogError("[BattleStartFlowDebugTest] BattleTestBootstrap reference is missing.");
            return false;
        }
    }
}
