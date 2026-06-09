using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only UI SFX helper. Supports button and menu feedback sounds.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class UISFXController : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private AudioSource audioSource;

        [Header("End Turn")]
        [SerializeField] private AudioClip[] endTurnClips;
        [Range(0f, 1f)]
        [SerializeField] private float endTurnVolume = 1f;
        [SerializeField] private Vector2 endTurnPitchRange = new Vector2(0.95f, 1.05f);

        // Future: add hoverClips, confirmClips, cancelClips, errorClips with matching Play methods.

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

        /// <summary>Plays a randomized End Turn button click clip with pitch variation.</summary>
        public void PlayEndTurn()
        {
            PlayRandomVariation(endTurnClips, endTurnVolume, endTurnPitchRange);
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
