using UnityEngine;

namespace CardBattle.Core
{
    public class RuntimeEncounterContextDebugTest : MonoBehaviour
    {
        [SerializeField] private RuntimeEncounterContext context;
        [SerializeField] private EncounterData encounterToSelect;
        [SerializeField] private string encounterIdToSelect;

        public int SelectedEventCount { get; private set; }
        public int ClearedEventCount { get; private set; }

        private RuntimeEncounterContext subscribedContext;

        private void OnEnable()
        {
            RefreshSubscription();
        }

        private void OnDisable()
        {
            UnsubscribeContext();
        }

        private void RefreshSubscription()
        {
            UnsubscribeContext();

            if (context == null)
                return;

            subscribedContext = context;
            subscribedContext.OnEncounterSelected += HandleEncounterSelected;
            subscribedContext.OnEncounterCleared += HandleEncounterCleared;
        }

        private void UnsubscribeContext()
        {
            if (subscribedContext == null)
                return;

            subscribedContext.OnEncounterSelected -= HandleEncounterSelected;
            subscribedContext.OnEncounterCleared -= HandleEncounterCleared;
            subscribedContext = null;
        }

        private void HandleEncounterSelected(EncounterData encounter)
        {
            SelectedEventCount++;
            Debug.Log(
                $"[RuntimeEncounterContextDebugTest] OnEncounterSelected " +
                $"(count={SelectedEventCount}) | Id={encounter?.EncounterId}");
        }

        private void HandleEncounterCleared()
        {
            ClearedEventCount++;
            Debug.Log(
                $"[RuntimeEncounterContextDebugTest] OnEncounterCleared " +
                $"(count={ClearedEventCount}).");
        }

        [ContextMenu("Debug Select Encounter Asset")]
        private void DebugSelectEncounterAsset()
        {
            if (!TryGetContext())
                return;

            if (encounterToSelect == null)
            {
                Debug.LogError(
                    "[RuntimeEncounterContextDebugTest] encounterToSelect reference is missing.");
                return;
            }

            bool selected = context.TrySelectEncounter(encounterToSelect);
            Debug.Log($"[RuntimeEncounterContextDebugTest] TrySelectEncounter => {selected}");
            DebugPrintContext();
        }

        [ContextMenu("Debug Select Encounter By ID")]
        private void DebugSelectEncounterById()
        {
            if (!TryGetContext())
                return;

            bool selected = context.TrySelectEncounterById(encounterIdToSelect);
            Debug.Log($"[RuntimeEncounterContextDebugTest] TrySelectEncounterById => {selected}");
            DebugPrintContext();
        }

        [ContextMenu("Debug Validate Current Encounter")]
        private void DebugValidateCurrentEncounter()
        {
            if (!TryGetContext())
                return;

            bool isValid = context.ValidateCurrentEncounter();
            Debug.Log($"[RuntimeEncounterContextDebugTest] ValidateCurrentEncounter => {isValid}");
            DebugPrintContext();
        }

        [ContextMenu("Debug Clear Current Encounter")]
        private void DebugClearCurrentEncounter()
        {
            if (!TryGetContext())
                return;

            context.ClearCurrentEncounter();
            DebugPrintContext();
        }

        [ContextMenu("Debug Print Runtime Encounter Context")]
        private void DebugPrintContext()
        {
            if (!TryGetContext())
                return;

            EncounterData encounter = context.CurrentEncounter;

            Debug.Log(
                $"[RuntimeEncounterContextDebugTest] --- Runtime Encounter Context ---\n" +
                $"HasCurrentEncounter={context.HasCurrentEncounter}\n" +
                $"CurrentEncounterId={context.CurrentEncounterId}\n" +
                $"DisplayName={(encounter != null ? encounter.DisplayName : "n/a")}\n" +
                $"EncounterType={context.CurrentEncounterType}\n" +
                $"EnvironmentId={context.CurrentEnvironmentId}\n" +
                $"EnvironmentSceneName={context.CurrentEnvironmentSceneName}\n" +
                $"RewardConfig={(context.CurrentRewardConfig != null ? context.CurrentRewardConfig.name : "null")}\n" +
                $"ValidEnemyCount={context.CurrentValidEnemyCount}\n" +
                $"IsCurrentEncounterValid={context.IsCurrentEncounterValid}\n" +
                $"LastValidationError={context.LastValidationError}\n" +
                $"SelectionCount={context.SelectionCount}\n" +
                $"ClearCount={context.ClearCount}\n" +
                $"SelectedEventCount={SelectedEventCount}\n" +
                $"ClearedEventCount={ClearedEventCount}");
        }

        private bool TryGetContext()
        {
            if (context != null)
                return true;

            Debug.LogError(
                "[RuntimeEncounterContextDebugTest] RuntimeEncounterContext reference is missing.");
            return false;
        }
    }
}
