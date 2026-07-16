using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Play Mode harness for generated Temporary cards and removed-card accounting.
    /// </summary>
    public class GeneratedTemporaryCardDebugTest : MonoBehaviour
    {
        private const string LogPrefix = "[GeneratedTemporaryDebug]";

        [Header("References")]
        [SerializeField] private DeckController deckController;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private BattleActionRunner battleActionRunner;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private PileCounterUI pileCounterUI;
        [SerializeField] private CardData generatorCard;
        [SerializeField] private CardData temporaryCard;

        [Header("Accounting")]
        [SerializeField] private int expectedGeneratedCount;

        private int? runtimeTotalBaseline;

        [ContextMenu("Debug/Print Generated Card State")]
        public void DebugPrintGeneratedCardState()
        {
            if (!ValidateDeck())
                return;

            var sb = new StringBuilder();
            sb.AppendLine($"{LogPrefix} Pile state:");
            sb.AppendLine($"Deck={deckController.Deck.Count}");
            sb.AppendLine($"Hand={deckController.Hand.Count}/{deckController.MaxHandSize}");
            sb.AppendLine($"Graveyard={deckController.Graveyard.Count}");
            sb.AppendLine($"Exhaust={deckController.GetExhaustCount()}");
            sb.AppendLine($"Removed={deckController.GetRemovedCount()}");
            sb.AppendLine($"PendingOverflow={deckController.PendingOverflowCount}");
            sb.AppendLine($"Total={GetRuntimeTotal()}");
            sb.AppendLine(
                $"RunnerBusy={(battleActionRunner != null && battleActionRunner.IsBusy)} " +
                $"CanAct={(player != null && player.CanAct)} " +
                $"EnemyResolving={(enemyActionSystem != null && enemyActionSystem.IsResolvingEnemyActions)}");

            sb.AppendLine($"Hand cards ({deckController.Hand.Count}):");
            AppendCardList(sb, deckController.Hand, includeKeywords: true);

            sb.AppendLine($"Removed cards ({deckController.RemovedCards.Count}):");
            AppendCardList(sb, deckController.RemovedCards, includeKeywords: false);

            Debug.Log(sb.ToString());
        }

        [ContextMenu("Debug/Create 1 Temporary Card Directly")]
        public void DebugCreate1TemporaryCardDirectly()
        {
            CreateAndLog(1);
        }

        [ContextMenu("Debug/Create 2 Temporary Cards Directly")]
        public void DebugCreate2TemporaryCardsDirectly()
        {
            CreateAndLog(2);
        }

        [ContextMenu("Debug/Fill Hand Then Create 2")]
        public void DebugFillHandThenCreate2()
        {
            if (!ValidateDeck())
                return;

            int safetyLimit = GetRuntimeTotal() + 2;
            int iterations = 0;
            while (deckController.Hand.Count < Mathf.Max(0, deckController.MaxHandSize - 1))
            {
                if (deckController.GetDeckCount() == 0 && deckController.GetGraveyardCount() == 0)
                    break;

                var step = deckController.DrawCards(1);
                if (step.DrawnCount <= 0)
                    break;

                iterations++;
                if (iterations > safetyLimit)
                {
                    Debug.LogError($"{LogPrefix} Fill Hand aborted — safety limit reached.");
                    break;
                }
            }

            var result = deckController.CreateCardsInHand(temporaryCard, 2);
            RefreshUi();
            Debug.Log(
                $"{LogPrefix} FillHandThenCreate2 | Hand={deckController.Hand.Count}/{deckController.MaxHandSize} " +
                $"AddedToHand={result.AddedToHandCount} RemovedByOverflow={result.RemovedByOverflowCount}");
            DebugPrintGeneratedCardState();
        }

        [ContextMenu("Debug/Validate No Duplicate InstanceIds")]
        public void DebugValidateNoDuplicateInstanceIds()
        {
            if (!ValidateDeck())
                return;

            if (!HasDuplicateInstanceIdsAcrossPiles())
                Debug.Log($"{LogPrefix} PASS — No duplicate InstanceIds across Deck/Hand/Graveyard/Exhaust/Removed");
            else
                Debug.LogError($"{LogPrefix} FAIL — Duplicate InstanceIds detected across piles");
        }

        [ContextMenu("Debug/Capture Runtime Total Baseline")]
        public void DebugCaptureRuntimeTotalBaseline()
        {
            if (!ValidateDeck())
                return;

            runtimeTotalBaseline = GetRuntimeTotal();
            Debug.Log($"{LogPrefix} Baseline captured: total={runtimeTotalBaseline.Value}");
        }

        [ContextMenu("Debug/Validate Runtime Total")]
        public void DebugValidateRuntimeTotal()
        {
            if (!ValidateDeck())
                return;

            int total = GetRuntimeTotal();
            bool noDup = !HasDuplicateInstanceIdsAcrossPiles();
            bool baselineOk = !runtimeTotalBaseline.HasValue ||
                              total == runtimeTotalBaseline.Value + Mathf.Max(0, expectedGeneratedCount);

            if (noDup && baselineOk)
            {
                Debug.Log(
                    $"{LogPrefix} PASS — total={total} baseline=" +
                    $"{(runtimeTotalBaseline.HasValue ? runtimeTotalBaseline.Value.ToString() : "none")} " +
                    $"expectedGenerated={Mathf.Max(0, expectedGeneratedCount)}");
            }
            else
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL — total={total} noDup={noDup} baseline=" +
                    $"{(runtimeTotalBaseline.HasValue ? runtimeTotalBaseline.Value.ToString() : "none")} " +
                    $"expectedGenerated={Mathf.Max(0, expectedGeneratedCount)} baselineOk={baselineOk}");
            }
        }

        [ContextMenu("Debug/Validate Temporary Pending Overflow Flush")]
        public void DebugValidateTemporaryPendingOverflowFlush()
        {
            if (!ValidateDeck())
                return;

            if (temporaryCard == null)
            {
                Debug.LogError($"{LogPrefix} Assign temporaryCard (Temporary=true) for this regression test.");
                return;
            }

            if (!temporaryCard.Temporary)
            {
                Debug.LogError($"{LogPrefix} temporaryCard must have Temporary=true.");
                return;
            }

            var deckBlueprint = new List<CardData>();
            for (int i = 0; i < 12; i++)
                deckBlueprint.Add(temporaryCard);

            deckController.BuildFromCardDataList(deckBlueprint);

            int safetyLimit = deckController.MaxHandSize + 4;
            int iterations = 0;
            while (deckController.Hand.Count < deckController.MaxHandSize)
            {
                var step = deckController.DrawCardsFromDeckImmediate(1);
                if (step.DrawnCount <= 0)
                    break;

                iterations++;
                if (iterations > safetyLimit)
                {
                    Debug.LogError($"{LogPrefix} FAIL — could not fill hand to MaxHandSize.");
                    return;
                }
            }

            if (deckController.Hand.Count != deckController.MaxHandSize)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL — expected full hand before overflow draw. " +
                    $"Hand={deckController.Hand.Count}/{deckController.MaxHandSize}");
                return;
            }

            int removedBefore = deckController.GetRemovedCount();
            int graveyardBefore = deckController.Graveyard.Count;
            int totalBefore = GetRuntimeTotal();

            var deckBeforeOverflow = deckController.Deck;
            if (deckBeforeOverflow.Count < 2)
            {
                Debug.LogError($"{LogPrefix} FAIL — expected at least 2 deck cards before overflow draw.");
                return;
            }

            var overflowIdA = deckBeforeOverflow[deckBeforeOverflow.Count - 1].InstanceId;
            var overflowIdB = deckBeforeOverflow[deckBeforeOverflow.Count - 2].InstanceId;

            var overflowDraw = deckController.DrawCardsFromDeckImmediate(2);
            if (overflowDraw.DrawnCount != 2 ||
                overflowDraw.AddedToHandCount != 0 ||
                overflowDraw.OverflowedToGraveyardCount != 2 ||
                deckController.PendingOverflowCount != 2)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL — overflow setup\n" +
                    $"Drawn={overflowDraw.DrawnCount} AddedToHand={overflowDraw.AddedToHandCount} " +
                    $"Overflowed={overflowDraw.OverflowedToGraveyardCount} " +
                    $"Pending={deckController.PendingOverflowCount}");
                return;
            }

            int flushed = deckController.FlushPendingOverflowToGraveyard();
            bool pendingEmpty = deckController.PendingOverflowCount == 0;
            int removedAfter = deckController.GetRemovedCount();
            int graveyardAfter = deckController.Graveyard.Count;
            int totalAfter = GetRuntimeTotal();
            bool noDup = !HasDuplicateInstanceIdsAcrossPiles();
            bool bothRemoved = removedAfter == removedBefore + 2;
            bool graveyardUnchanged = graveyardAfter == graveyardBefore;
            bool totalConserved = totalAfter == totalBefore;
            bool flushCountOk = flushed == 2;
            bool idAInRemovedOnce = CountInstanceIdInRemoved(overflowIdA) == 1;
            bool idBInRemovedOnce = CountInstanceIdInRemoved(overflowIdB) == 1;
            bool idANotInGraveyard = CountInstanceIdInGraveyard(overflowIdA) == 0;
            bool idBNotInGraveyard = CountInstanceIdInGraveyard(overflowIdB) == 0;

            if (pendingEmpty && bothRemoved && graveyardUnchanged && totalConserved && noDup &&
                flushCountOk && idAInRemovedOnce && idBInRemovedOnce &&
                idANotInGraveyard && idBNotInGraveyard)
            {
                Debug.Log(
                    $"{LogPrefix} PASS — Temporary pending overflow flush\n" +
                    $"Flushed={flushed} Removed={removedAfter} Graveyard={graveyardAfter} " +
                    $"Total={totalAfter} Pending={deckController.PendingOverflowCount}");
            }
            else
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL — Temporary pending overflow flush\n" +
                    $"Flushed={flushed} PendingEmpty={pendingEmpty} BothRemoved={bothRemoved} " +
                    $"GraveyardUnchanged={graveyardUnchanged} TotalConserved={totalConserved} NoDup={noDup} " +
                    $"IdAInRemovedOnce={idAInRemovedOnce} IdBInRemovedOnce={idBInRemovedOnce} " +
                    $"IdANotInGraveyard={idANotInGraveyard} IdBNotInGraveyard={idBNotInGraveyard}");
            }

            RefreshUi();
            DebugPrintGeneratedCardState();
        }

        private int CountInstanceIdInRemoved(System.Guid id)
        {
            return CountInstanceIdInPile(deckController.RemovedCards, id);
        }

        private int CountInstanceIdInGraveyard(System.Guid id)
        {
            return CountInstanceIdInPile(deckController.Graveyard, id);
        }

        private static int CountInstanceIdInPile(IReadOnlyList<CardInstance> pile, System.Guid id)
        {
            int count = 0;
            if (pile == null)
                return 0;

            for (int i = 0; i < pile.Count; i++)
            {
                if (pile[i] != null && pile[i].InstanceId == id)
                    count++;
            }

            return count;
        }

        [ContextMenu("Debug/Print Manual Checklist")]
        public void DebugPrintManualChecklist()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{LogPrefix} Generated Temporary checklist:");
            sb.AppendLine("1. Play Hidden Weapon. AP decreases once and two unique Stone instances appear in Hand.");
            sb.AppendLine("2. Generated Stones do not change Deck count and create new Hand views only.");
            sb.AppendLine("3. Play Stone: damage resolves, Stone leaves Hand, enters Removed, no Graveyard/Exhaust increase.");
            sb.AppendLine("4. Stone play does not use Graveyard VFX, but still triggers enemy countdown once.");
            sb.AppendLine("5. Manual discard a Stone: it leaves Hand and enters Removed, not Graveyard.");
            sb.AppendLine("6. End turn with Stone in Hand: non-Retain Stone is removed; normal cards go to Graveyard.");
            sb.AppendLine("7. Retain + Temporary stays in Hand at end turn, then later play removes it.");
            sb.AppendLine("8. With Hand=9, create 2 Stones: AddedToHand=1, RemovedByOverflow=1.");
            sb.AppendLine("9. With Hand=10, create 2 Stones: AddedToHand=0, RemovedByOverflow=2.");
            sb.AppendLine("10. Capture baseline, generate cards, and validate total = baseline + generated count.");
            sb.AppendLine("11. Start a new battle: Removed resets to 0 and generated cards do not persist.");

            if (generatorCard != null)
                sb.AppendLine($"Generator card: {generatorCard.DisplayName} (id={generatorCard.CardId})");

            if (temporaryCard != null)
            {
                sb.AppendLine($"Temporary card: {temporaryCard.DisplayName} (id={temporaryCard.CardId})");
                sb.AppendLine(
                    $"Temporary={temporaryCard.Temporary} Retain={temporaryCard.Retain} " +
                    $"ExhaustAfterPlay={temporaryCard.ExhaustAfterPlay}");
                sb.AppendLine($"Description:\n{CardDescriptionBuilder.Build(temporaryCard)}");
            }

            Debug.Log(sb.ToString());
            DebugPrintGeneratedCardState();
        }

        private void CreateAndLog(int amount)
        {
            if (!ValidateDeck())
                return;

            var result = deckController.CreateCardsInHand(temporaryCard, amount);
            RefreshUi();
            Debug.Log(
                $"{LogPrefix} CreateCardsInHand({amount}) | Requested={result.RequestedCount} " +
                $"Created={result.CreatedCount} AddedToHand={result.AddedToHandCount} " +
                $"RemovedByOverflow={result.RemovedByOverflowCount}");
            DebugPrintGeneratedCardState();
        }

        private void AppendCardList(StringBuilder sb, IReadOnlyList<CardInstance> cards, bool includeKeywords)
        {
            if (cards == null)
                return;

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card?.Data == null)
                {
                    sb.AppendLine($"  [{i}] <null>");
                    continue;
                }

                if (includeKeywords)
                {
                    sb.AppendLine(
                        $"  [{i}] {card.Data.DisplayName} | id={card.InstanceId} | " +
                        $"Temporary={DeckController.ResolveTemporary(card)} | " +
                        $"Retain={DeckController.ResolveRetainAtEndTurn(card)} | " +
                        $"ExhaustAfterPlay={card.Data.ExhaustAfterPlay}");
                }
                else
                {
                    sb.AppendLine($"  [{i}] {card.Data.DisplayName} | id={card.InstanceId}");
                }
            }
        }

        private int GetRuntimeTotal()
        {
            if (deckController == null)
                return 0;

            return deckController.Deck.Count
                   + deckController.Hand.Count
                   + deckController.Graveyard.Count
                   + deckController.GetExhaustCount()
                   + deckController.GetRemovedCount()
                   + deckController.PendingOverflowCount;
        }

        private bool HasDuplicateInstanceIdsAcrossPiles()
        {
            var seen = new HashSet<System.Guid>();
            return HasDupInPile(deckController.Deck, seen) ||
                   HasDupInPile(deckController.Hand, seen) ||
                   HasDupInPile(deckController.Graveyard, seen) ||
                   HasDupInPile(deckController.ExhaustPile, seen) ||
                   HasDupInPile(deckController.RemovedCards, seen);
        }

        private static bool HasDupInPile(IReadOnlyList<CardInstance> pile, HashSet<System.Guid> seen)
        {
            if (pile == null)
                return false;

            for (int i = 0; i < pile.Count; i++)
            {
                var card = pile[i];
                if (card == null)
                    continue;
                if (!seen.Add(card.InstanceId))
                    return true;
            }

            return false;
        }

        private void RefreshUi()
        {
            handUIController?.SyncHandViewsExternal();
            pileCounterUI?.ForceSyncDisplayedToReal();
        }

        private bool ValidateDeck()
        {
            if (deckController != null)
                return true;

            Debug.LogError($"{LogPrefix} DeckController reference is missing.");
            return false;
        }
    }
}
