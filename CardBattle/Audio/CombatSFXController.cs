using UnityEngine;
using UnityEngine.Serialization;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only combat SFX helper. Plays attack-hit sounds at real damage moments.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CombatSFXController : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private AudioSource audioSource;

        [Header("Attack Hit")]
        [FormerlySerializedAs("swordHitClips")]
        [SerializeField] private AudioClip[] attackHitClips;
        [FormerlySerializedAs("swordHitVolume")]
        [Range(0f, 1f)]
        [SerializeField] private float attackHitVolume = 1f;
        [FormerlySerializedAs("swordHitPitchRange")]
        [SerializeField] private Vector2 attackHitPitchRange = new Vector2(0.95f, 1.05f);

        // Future: add attackBlockClips + PlayAttackBlock() using the same PlayRandomVariation helper.

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                return;

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        /// <summary>Plays a randomized attack-hit clip with pitch variation.</summary>
        public void PlayAttackHit()
        {
            PlayRandomVariation(attackHitClips, attackHitVolume, attackHitPitchRange);
        }

        private void PlayRandomVariation(AudioClip[] clips, float volume, Vector2 pitchRange)
        {
            if (audioSource == null || clips == null || clips.Length == 0)
                return;

            AudioClip clip = null;
            int attempts = clips.Length;
            while (attempts-- > 0)
            {
                AudioClip candidate = clips[Random.Range(0, clips.Length)];
                if (candidate != null)
                {
                    clip = candidate;
                    break;
                }
            }

            if (clip == null)
                return;

            float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }
}
