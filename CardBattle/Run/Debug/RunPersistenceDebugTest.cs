using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardBattle.Core
{
    public class RunPersistenceDebugTest : MonoBehaviour
    {
        [ContextMenu("Start Test Run")]
        public void StartTestRun()
        {
            var starterDeck = new List<RunCardRecord>
            {
                new RunCardRecord("strike"),
                new RunCardRecord("strike"),
                new RunCardRecord("block"),
                new RunCardRecord("strike")
            };

            bool success = RunManager.Instance.StartNewRun(
                "run_persistence_test",
                12345,
                "knight",
                80,
                starterDeck
            );

            RunManager.Instance.CurrentRun.maxHp = 120;
            RunManager.Instance.SetCurrentHp(42);
            RunManager.Instance.AddGold(75);
            RunManager.Instance.AddCard("AllStrike");

            Debug.Log(
                $"[Persistence Test] Started={success} | " +
                $"Class={RunManager.Instance.CurrentRun.playerClassId} | " +
                $"HP={RunManager.Instance.CurrentRun.currentHp}/" +
                $"{RunManager.Instance.CurrentRun.maxHp} | " +
                $"Gold={RunManager.Instance.CurrentRun.gold} | " +
                $"Deck={RunManager.Instance.CurrentRun.currentDeck.Count}"
            );
        }

        [ContextMenu("Load Test Scene A")]
        public void LoadSceneA()
        {
            SceneManager.LoadScene("RunPersistence_TestA");
        }

        [ContextMenu("Load Test Scene B")]
        public void LoadSceneB()
        {
            SceneManager.LoadScene("RunPersistence_TestB");
        }

        [ContextMenu("Print Current Run")]
        public void PrintCurrentRun()
        {
            if (RunManager.Instance == null)
            {
                Debug.LogError("[Persistence Test] RunManager.Instance is null.");
                return;
            }

            RunState run = RunManager.Instance.CurrentRun;

            Debug.Log(
                $"[Persistence Test] Scene={SceneManager.GetActiveScene().name} | " +
                $"Active={RunManager.Instance.HasActiveRun} | " +
                $"Class={run.playerClassId} | " +
                $"HP={run.currentHp}/{run.maxHp} | " +
                $"Gold={run.gold} | " +
                $"Deck={run.currentDeck.Count}"
            );
        }
    }
}