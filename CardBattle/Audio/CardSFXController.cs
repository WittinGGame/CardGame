using UnityEngine;
using UnityEngine.Serialization;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only card SFX helper. Plays draw, play, and hover sounds with clip/pitch variation.
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

        [Header("Attack Play")]
        [SerializeField] private AudioClip[] attackPlayClips;
        [Range(0f, 1f)]
        [SerializeField] private float attackPlayVolume = 1f;
        [SerializeField] private Vector2 attackPlayPitchRange = new Vector2(0.95f, 1.05f);

        [Header("Defend Play")]
        [SerializeField] private AudioClip[] defendPlayClips;
        [Range(0f, 1f)]
        [SerializeField] private float defendPlayVolume = 1f;
        [SerializeField] private Vector2 defendPlayPitchRange = new Vector2(0.95f, 1.05f);

        [Header("Buff Play")]
        [SerializeField] private AudioClip[] buffPlayClips;
        [Range(0f, 1f)]
        [SerializeField] private float buffPlayVolume = 1f;
        [SerializeField] private Vector2 buffPlayPitchRange = new Vector2(0.95f, 1.05f);

        [Header("Heal Play")]
        [SerializeField] private AudioClip[] healPlayClips;
        [Range(0f, 1f)]
        [SerializeField] private float healPlayVolume = 1f;
        [SerializeField] private Vector2 healPlayPitchRange = new Vector2(0.95f, 1.05f);

        [Header("Default Play")]
        [FormerlySerializedAs("playClips")]
        [SerializeField] private AudioClip[] defaultPlayClips;
        [FormerlySerializedAs("playVolume")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultPlayVolume = 1f;
        [FormerlySerializedAs("playPitchRange")]
        [SerializeField] private Vector2 defaultPlayPitchRange = new Vector2(0.95f, 1.05f);

        [Header("Card Hover")]
        [SerializeField] private AudioClip[] hoverClips;
        [Range(0f, 1f)]
        [SerializeField] private float hoverVolume = 0.5f;
        [SerializeField] private Vector2 hoverPitchRange = new Vector2(0.98f, 1.02f);

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
            TryPlayRandomVariation(drawClips, drawVolume, drawPitchRange);
        }

        public void PlayCardPlayed(CardType cardType)
        {
            if (TryPlayCardTypeVariation(cardType))
                return;

            TryPlayRandomVariation(defaultPlayClips, defaultPlayVolume, defaultPlayPitchRange);
        }

        public void PlayHover()
        {
            TryPlayRandomVariation(hoverClips, hoverVolume, hoverPitchRange);
        }

        private bool TryPlayCardTypeVariation(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Attack:
                    return TryPlayRandomVariation(attackPlayClips, attackPlayVolume, attackPlayPitchRange);
                case CardType.Defend:
                    return TryPlayRandomVariation(defendPlayClips, defendPlayVolume, defendPlayPitchRange);
                case CardType.Buff:
                    return TryPlayRandomVariation(buffPlayClips, buffPlayVolume, buffPlayPitchRange);
                case CardType.Heal:
                    return TryPlayRandomVariation(healPlayClips, healPlayVolume, healPlayPitchRange);
                default:
                    return false;
            }
        }

        private bool TryPlayRandomVariation(AudioClip[] clips, float volume, Vector2 pitchRange)
        {
            if (audioSource == null || clips == null || clips.Length == 0)
                return false;

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
                return false;

            float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            return true;
        }
    }
}
