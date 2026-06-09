## FILE: CardSFXController.cs
**Path:** `Assets/Scripts/CardBattle/Audio/CardSFXController.cs`
```csharp
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
```

## FILE: CombatSFXController.cs
**Path:** `Assets/Scripts/CardBattle/Audio/CombatSFXController.cs`
```csharp
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
```

## FILE: UISFXController.cs
**Path:** `Assets/Scripts/CardBattle/Audio/UISFXController.cs`
```csharp
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
```