using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only card SFX helper. Plays draw and play sounds with clip/pitch variation.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CardSFXController : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private AudioSource audioSource;

        [Header("Draw Card")]
        [SerializeField] private AudioClip[] drawClips;
        [Range(0f, 1f)]
        [SerializeField] private float drawVolume = 1f;
        [SerializeField] private Vector2 drawPitchRange = new Vector2(0.95f, 1.05f);

        [Header("Play Card")]
        [SerializeField] private AudioClip[] playClips;
        [Range(0f, 1f)]
        [SerializeField] private float playVolume = 1f;
        [SerializeField] private Vector2 playPitchRange = new Vector2(0.95f, 1.05f);

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

        public void PlayDraw()
        {
            PlayRandomVariation(drawClips, drawVolume, drawPitchRange);
        }

        public void PlayCardPlayed()
        {
            PlayRandomVariation(playClips, playVolume, playPitchRange);
        }

        private void PlayRandomVariation(AudioClip[] clips, float volume, Vector2 pitchRange)
        {
            if (audioSource == null || clips == null || clips.Length == 0)
                return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null)
                return;

            float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }
}
