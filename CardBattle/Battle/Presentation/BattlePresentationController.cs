using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Scene-level bridge for battle presentation systems that subscribe to runtime enemy events.
    /// </summary>
    public class BattlePresentationController : MonoBehaviour
    {
        [SerializeField] private BattleCameraFeedbackController cameraFeedback;

        private void Awake()
        {
            if (cameraFeedback == null)
                cameraFeedback = GetComponent<BattleCameraFeedbackController>();
        }

        public void RefreshSubscriptions()
        {
            cameraFeedback?.RefreshEnemySubscriptions();
        }
    }
}
