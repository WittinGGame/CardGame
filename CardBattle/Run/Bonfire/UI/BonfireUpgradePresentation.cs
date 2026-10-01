using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    // Presentation only. Gameplay state, offer generation, selection and saving stay in BonfireController.
    public class BonfireUpgradePresentation : MonoBehaviour
    {
        public enum PresentationPhase { Idle, Intro, CenterHold, ChoiceExpand, ChoiceWait, ChoiceCollapse, Exit }

        [Header("R5A Intro")]
        [SerializeField] private CanvasGroup[] fadeGroups;
        [SerializeField] private RectTransform motionRoot;
        [SerializeField] private RectTransform centerTarget;
        [SerializeField, Min(0f)] private float fadeDuration = .25f;
        [SerializeField, Min(0f)] private float moveDuration = .6f;

        [Header("Mutation VFX")]
        [SerializeField] private GameObject curseVFXFront;
        [SerializeField, Min(0f)] private float mutationVFXDelay = .12f;
        [SerializeField, Min(0f)] private float mutationVFXDuration = .4f;

        [Header("Mutation Choices")]
        [SerializeField] private UpgradeCardChoiceView[] mutationChoiceCards;
        [SerializeField] private RectTransform[] mutationChoiceRoots;
        [SerializeField] private CanvasGroup[] mutationChoiceGroups;
        [SerializeField] private RectTransform mutationChoiceStartAnchor;
        [SerializeField] private RectTransform leftChoiceAnchor;
        [SerializeField] private RectTransform centerChoiceAnchor;
        [SerializeField] private RectTransform rightChoiceAnchor;
        [SerializeField] private RectTransform[] extendedChoiceAnchors;
        [SerializeField, Min(0f)] private float choiceExpandDuration = .4f;
        [SerializeField, Range(.1f, 1f)] private float choiceStartScale = .72f;
        [SerializeField, Min(0f)] private float holdDelay = .15f;

        [Header("Mutation Choice Prompt")]
        [SerializeField] private CanvasGroup mutationChoicePrompt;
        [SerializeField, Min(0f)] private float mutationPromptFadeInDelay = .05f;
        [SerializeField, Min(0f)] private float mutationPromptFadeInDuration = .2f;
        [SerializeField, Min(0f)] private float mutationPromptFadeOutDuration = .15f;

        [Header("Exit")]
        [SerializeField] private CanvasGroup presentationBackground;
        [SerializeField] private BonfirePresentationBackgroundDissolve backgroundDissolve;
        [SerializeField] private CanvasGroup motionCanvasGroup;
        [SerializeField] private RectTransform upgradeExitAnchor;
        [SerializeField, Min(0f)] private float unselectedFadeDuration = .2f;
        [SerializeField, Min(0f)] private float backgroundFadeDuration = .45f;
        [SerializeField, Min(0f)] private float cardExitDelay = .15f;
        [SerializeField, Min(0f)] private float exitMoveDuration = .65f;
        [SerializeField, Min(0f)] private float exitFadeDuration = .45f;

        public bool IsActive { get; private set; }
        public bool IsHolding { get; private set; }
        public PresentationPhase Phase { get; private set; }
        public int ActiveChoiceCount { get; private set; }
        public string SelectedMutationId { get; private set; } = string.Empty;
        public bool LastExitReachedAnchor { get; private set; }
        public bool LastExitFadedBackground { get; private set; }
        public event Action Held;
        public event Action Finished;

        private BonfireController controller;
        private string pendingCardId;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float[] initialAlphas;
        private bool[] initialInteractable;
        private float initialBackgroundAlpha;
        private float initialMotionAlpha;
        private float elapsed;
        private int selectedChoice = -1;
        private Vector3[] choiceStartPositions;
        private Vector3[] choiceTargetPositions;
        private Vector3 collapseStartPosition;
        private RectTransform exitRoot;
        private CanvasGroup exitGroup;
        private Vector3 exitStartPosition;
        private bool mutationVFXTimerActive;
        private float mutationVFXElapsed;
        private enum PromptFadeMode { None, In, Out }
        private PromptFadeMode promptFadeMode;
        private float promptFadeElapsed;
        private float promptFadeStartAlpha;

        public bool IsConfigured => isActiveAndEnabled && motionRoot != null && centerTarget != null &&
            upgradeExitAnchor != null && presentationBackground != null && motionCanvasGroup != null &&
            mutationChoiceStartAnchor != null && leftChoiceAnchor != null && centerChoiceAnchor != null && rightChoiceAnchor != null &&
            mutationChoiceCards != null && mutationChoiceRoots != null && mutationChoiceGroups != null &&
            fadeGroups != null;

        public bool TryBegin(UpgradeResolution result)
        {
            return result != null && Begin(null, result.RunCardInstanceId);
        }

        public bool TryBeginMutationChoice(BonfireController source, string runCardInstanceId)
        {
            var committed = source != null ? source.GetCommittedUpgradeCardSnapshot() : null;
            return source != null && source.IsWaitingForMutationChoice && committed != null &&
                committed.runCardInstanceId == runCardInstanceId &&
                Begin(source, runCardInstanceId);
        }

        private bool Begin(BonfireController source, string runCardInstanceId)
        {
            if (!IsConfigured || IsActive || string.IsNullOrWhiteSpace(runCardInstanceId)) return false;
            controller = source;
            pendingCardId = runCardInstanceId;
            initialPosition = motionRoot.localPosition;
            initialRotation = motionRoot.localRotation;
            initialAlphas = new float[fadeGroups.Length];
            initialInteractable = new bool[fadeGroups.Length];
            for (int i = 0; i < fadeGroups.Length; i++)
            {
                if (fadeGroups[i] == null) continue;
                initialAlphas[i] = fadeGroups[i].alpha;
                initialInteractable[i] = fadeGroups[i].interactable;
                fadeGroups[i].interactable = false;
            }
            initialBackgroundAlpha = presentationBackground.alpha;
            initialMotionAlpha = motionCanvasGroup.alpha;
            presentationBackground.interactable = false;
            motionCanvasGroup.alpha = 1f;
            HideChoices();
            HideMutationPrompt();
            SubscribeChoices();
            elapsed = 0f;
            selectedChoice = -1;
            SelectedMutationId = string.Empty;
            LastExitReachedAnchor = false;
            LastExitFadedBackground = false;
            IsActive = true;
            IsHolding = false;
            Phase = PresentationPhase.Intro;
            CancelMutationVFX();
            return true;
        }

        private void Update() { Advance(Time.unscaledDeltaTime); }

        private void Advance(float deltaTime)
        {
            if (!IsActive) return;
            float step = Mathf.Max(0f, deltaTime);
            AdvanceMutationVFX(step);
            AdvanceMutationPrompt(step);
            elapsed += step;
            switch (Phase)
            {
                case PresentationPhase.Intro: AdvanceIntro(); break;
                case PresentationPhase.CenterHold: AdvanceCenterHold(); break;
                case PresentationPhase.ChoiceExpand: AdvanceChoiceExpand(); break;
                case PresentationPhase.ChoiceCollapse: AdvanceChoiceCollapse(); break;
                case PresentationPhase.Exit: AdvanceExit(); break;
            }
        }

        private void AdvanceIntro()
        {
            float fadeTime = Mathf.Max(0f, fadeDuration);
            float fade = fadeTime <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeTime);
            for (int i = 0; i < fadeGroups.Length; i++)
                if (fadeGroups[i] != null) fadeGroups[i].alpha = initialAlphas[i] * (1f - fade);
            if (elapsed < fadeTime) return;
            float motionTime = Mathf.Max(0f, moveDuration);
            float progress = motionTime <= 0f ? 1f : Mathf.Clamp01((elapsed - fadeTime) / motionTime);
            motionRoot.localPosition = Vector3.Lerp(initialPosition, LocalTarget(motionRoot, centerTarget), Mathf.SmoothStep(0f, 1f, progress));
            motionRoot.localRotation = initialRotation * Quaternion.AngleAxis(360f * progress, Vector3.up);
            if (progress < 1f) return;
            motionRoot.localRotation = initialRotation;
            elapsed = 0f;
            Phase = PresentationPhase.CenterHold;
            IsHolding = true;
            if (controller != null && controller.IsWaitingForMutationChoice)
                StartMutationVFX();
            Held?.Invoke();
        }

        private void AdvanceCenterHold()
        {
            float baseHold = Mathf.Max(0f, holdDelay);
            bool mutation = controller != null && controller.IsWaitingForMutationChoice;
            if (!mutation)
            {
                SetCurseVFXActive(false);
                if (elapsed < baseHold) return;
                IsHolding = false;
                BeginExit(motionRoot, motionCanvasGroup);
                return;
            }

            if (elapsed < baseHold) return;
            IsHolding = false;
            if (BeginChoices()) return;
            BeginExit(motionRoot, motionCanvasGroup);
        }

        private bool BeginChoices()
        {
            IReadOnlyList<string> offers = controller.GetOfferedMutationIds();
            int capacity = Mathf.Min(mutationChoiceCards?.Length ?? 0,
                Mathf.Min(mutationChoiceRoots?.Length ?? 0, mutationChoiceGroups?.Length ?? 0));
            ActiveChoiceCount = Mathf.Min(offers.Count, capacity);
            if (ActiveChoiceCount <= 0) return false;
            choiceStartPositions = new Vector3[ActiveChoiceCount];
            choiceTargetPositions = new Vector3[ActiveChoiceCount];
            for (int i = 0; i < ActiveChoiceCount; i++)
            {
                if (!controller.TryResolveOfferedMutation(offers[i], out var data, out var mutation, out var resolved))
                {
                    HideChoices();
                    return false;
                }
                var root = mutationChoiceRoots[i];
                var group = mutationChoiceGroups[i];
                var view = mutationChoiceCards[i];
                root.gameObject.SetActive(true);
                root.localPosition = LocalTarget(root, mutationChoiceStartAnchor);
                root.localScale = Vector3.one * choiceStartScale;
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                view.BindPresentationChoice(offers[i], data, CardDescriptionBuilder.BuildForInstance(resolved),
                    resolved.EffectiveApCost, mutation.DisplayName, false);
                choiceStartPositions[i] = root.localPosition;
                choiceTargetPositions[i] = LocalTarget(root, ChoiceAnchor(i, ActiveChoiceCount));
            }
            motionCanvasGroup.alpha = 1f;
            StartMutationPromptFadeIn();
            elapsed = 0f;
            Phase = PresentationPhase.ChoiceExpand;
            return true;
        }

        private void AdvanceChoiceExpand()
        {
            float duration = Mathf.Max(0f, choiceExpandDuration);
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            motionCanvasGroup.alpha = 1f - progress;
            for (int i = 0; i < ActiveChoiceCount; i++)
            {
                mutationChoiceRoots[i].localPosition = Vector3.Lerp(choiceStartPositions[i], choiceTargetPositions[i], eased);
                mutationChoiceRoots[i].localScale = Vector3.one * Mathf.Lerp(choiceStartScale, 1f, eased);
                mutationChoiceGroups[i].alpha = progress;
            }
            if (progress < 1f) return;
            for (int i = 0; i < ActiveChoiceCount; i++)
            {
                mutationChoiceGroups[i].interactable = true;
                mutationChoiceGroups[i].blocksRaycasts = true;
                mutationChoiceCards[i].SetPresentationInteractable(true);
            }
            elapsed = 0f;
            IsHolding = true;
            Phase = PresentationPhase.ChoiceWait;
        }

        private void HandleChoice(string id)
        {
            if (Phase != PresentationPhase.ChoiceWait || controller == null || !controller.CanSelectMutation) return;
            selectedChoice = -1;
            for (int i = 0; i < ActiveChoiceCount; i++)
            {
                mutationChoiceCards[i].SetPresentationInteractable(false);
                mutationChoiceGroups[i].interactable = false;
                mutationChoiceGroups[i].blocksRaycasts = false;
                if (mutationChoiceCards[i].OwnedCardId == id) selectedChoice = i;
            }
            if (selectedChoice < 0 || !controller.TrySelectMutation(id))
            {
                if (controller.CanSelectMutation)
                    for (int i = 0; i < ActiveChoiceCount; i++)
                    {
                        mutationChoiceCards[i].SetPresentationInteractable(true);
                        mutationChoiceGroups[i].interactable = true;
                        mutationChoiceGroups[i].blocksRaycasts = true;
                    }
                selectedChoice = -1;
                return;
            }
            SelectedMutationId = id;
            StartMutationPromptFadeOut();
            collapseStartPosition = mutationChoiceRoots[selectedChoice].localPosition;
            elapsed = 0f;
            IsHolding = false;
            Phase = PresentationPhase.ChoiceCollapse;
        }

        private void AdvanceChoiceCollapse()
        {
            float duration = Mathf.Max(0f, unselectedFadeDuration);
            float progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            for (int i = 0; i < ActiveChoiceCount; i++)
            {
                if (i == selectedChoice)
                    mutationChoiceRoots[i].localPosition = Vector3.Lerp(collapseStartPosition,
                        LocalTarget(mutationChoiceRoots[i], centerTarget), eased);
                else
                    mutationChoiceGroups[i].alpha = 1f - progress;
            }
            if (progress < 1f) return;
            for (int i = 0; i < ActiveChoiceCount; i++)
                if (i != selectedChoice) mutationChoiceRoots[i].gameObject.SetActive(false);
            BeginExit(mutationChoiceRoots[selectedChoice], mutationChoiceGroups[selectedChoice]);
        }

        private void BeginExit(RectTransform root, CanvasGroup group)
        {
            CancelMutationVFX();
            exitRoot = root;
            exitGroup = group;
            exitStartPosition = root.localPosition;
            group.alpha = 1f;
            elapsed = 0f;
            Phase = PresentationPhase.Exit;
            if (backgroundDissolve != null) backgroundDissolve.PlayFadeOut();
        }

        private void AdvanceExit()
        {
            float backgroundTime = backgroundDissolve != null
                ? Mathf.Max(0f, backgroundDissolve.FadeOutDuration)
                : Mathf.Max(0f, backgroundFadeDuration);
            float backgroundFade = backgroundTime <= 0f ? 1f : Mathf.Clamp01(elapsed / backgroundTime);
            float cardElapsed = Mathf.Max(0f, elapsed - Mathf.Max(0f, cardExitDelay));
            float moveTime = Mathf.Max(0f, exitMoveDuration);
            float fadeTime = Mathf.Max(0f, exitFadeDuration);
            float move = moveTime <= 0f ? 1f : Mathf.Clamp01(cardElapsed / moveTime);
            float fade = fadeTime <= 0f ? 1f : Mathf.Clamp01(cardElapsed / fadeTime);
            exitRoot.localPosition = Vector3.Lerp(exitStartPosition, LocalTarget(exitRoot, upgradeExitAnchor), Mathf.SmoothStep(0f, 1f, move));
            exitGroup.alpha = 1f - fade;
            if (backgroundDissolve == null)
                presentationBackground.alpha = initialBackgroundAlpha * (1f - backgroundFade);
            if (backgroundFade < 1f || move < 1f || fade < 1f) return;
            if (backgroundDissolve != null) backgroundDissolve.SetReveal(0f);
            Finish();
        }

        private RectTransform ChoiceAnchor(int index, int count)
        {
            if (count > 3 && extendedChoiceAnchors != null && extendedChoiceAnchors.Length >= count &&
                extendedChoiceAnchors[index] != null) return extendedChoiceAnchors[index];
            if (count <= 1) return centerChoiceAnchor;
            if (count == 2) return index == 0 ? leftChoiceAnchor : rightChoiceAnchor;
            if (index == 0) return leftChoiceAnchor;
            if (index == count - 1) return rightChoiceAnchor;
            return centerChoiceAnchor;
        }

        private static Vector3 LocalTarget(RectTransform root, RectTransform target)
        {
            return root.parent != null ? root.parent.InverseTransformPoint(target.position) : target.position;
        }

        private void SubscribeChoices()
        {
            if (mutationChoiceCards == null) return;
            foreach (var view in mutationChoiceCards)
                if (view != null) { view.OnSelected -= HandleChoice; view.OnSelected += HandleChoice; }
        }

        private void UnsubscribeChoices()
        {
            if (mutationChoiceCards == null) return;
            foreach (var view in mutationChoiceCards)
                if (view != null) view.OnSelected -= HandleChoice;
        }

        private void HideChoices()
        {
            ActiveChoiceCount = 0;
            if (mutationChoiceRoots == null) return;
            foreach (var root in mutationChoiceRoots) if (root != null) root.gameObject.SetActive(false);
        }

        private void Finish()
        {
            LastExitReachedAnchor = exitRoot != null && upgradeExitAnchor != null &&
                Vector3.Distance(exitRoot.position, upgradeExitAnchor.position) < .01f;
            LastExitFadedBackground = backgroundDissolve != null
                ? backgroundDissolve.Reveal <= .001f
                : presentationBackground != null && presentationBackground.alpha <= .001f;
            IsActive = false;
            IsHolding = false;
            Phase = PresentationPhase.Idle;
            Finished?.Invoke();
            RestoreVisuals(false);
        }

        public void ResetPresentation()
        {
            CancelMutationVFX();
            if (!IsActive && Phase == PresentationPhase.Idle) return;
            IsActive = false;
            IsHolding = false;
            Phase = PresentationPhase.Idle;
            RestoreVisuals(true);
        }

        private void RestoreVisuals(bool restoreBackground)
        {
            if (motionRoot != null) { motionRoot.localPosition = initialPosition; motionRoot.localRotation = initialRotation; }
            if (motionCanvasGroup != null) motionCanvasGroup.alpha = initialMotionAlpha;
            if (presentationBackground != null)
            {
                presentationBackground.alpha = initialBackgroundAlpha;
                presentationBackground.interactable = false;
            }
            if (restoreBackground && backgroundDissolve != null) backgroundDissolve.SetReveal(1f);
            if (fadeGroups != null && initialAlphas != null)
                for (int i = 0; i < fadeGroups.Length; i++)
                    if (fadeGroups[i] != null)
                    {
                        fadeGroups[i].alpha = initialAlphas[i];
                        fadeGroups[i].interactable = initialInteractable[i];
                    }
            HideChoices();
            HideMutationPrompt();
            CancelMutationVFX();
            controller = null;
            pendingCardId = string.Empty;
            SelectedMutationId = string.Empty;
            selectedChoice = -1;
        }

        private void SetCurseVFXActive(bool active)
        {
            if (curseVFXFront != null && curseVFXFront.activeSelf != active)
                curseVFXFront.SetActive(active);
        }

        private void StartMutationVFX()
        {
            CancelMutationVFX();
            if (curseVFXFront == null) return;
            mutationVFXTimerActive = true;
            mutationVFXElapsed = 0f;
            if (Mathf.Max(0f, mutationVFXDelay) <= 0f)
                SetCurseVFXActive(true);
        }

        private void AdvanceMutationVFX(float deltaTime)
        {
            if (!mutationVFXTimerActive) return;
            mutationVFXElapsed += deltaTime;
            float delay = Mathf.Max(0f, mutationVFXDelay);
            float duration = Mathf.Max(0f, mutationVFXDuration);
            if (mutationVFXElapsed < delay)
            {
                SetCurseVFXActive(false);
                return;
            }
            if (mutationVFXElapsed < delay + duration)
            {
                SetCurseVFXActive(true);
                return;
            }
            CancelMutationVFX();
        }

        private void CancelMutationVFX()
        {
            mutationVFXTimerActive = false;
            mutationVFXElapsed = 0f;
            SetCurseVFXActive(false);
        }

        private void StartMutationPromptFadeIn()
        {
            if (mutationChoicePrompt == null) return;
            mutationChoicePrompt.gameObject.SetActive(true);
            mutationChoicePrompt.alpha = 0f;
            mutationChoicePrompt.interactable = false;
            mutationChoicePrompt.blocksRaycasts = false;
            promptFadeMode = PromptFadeMode.In;
            promptFadeElapsed = 0f;
            promptFadeStartAlpha = 0f;
        }

        private void StartMutationPromptFadeOut()
        {
            if (mutationChoicePrompt == null) return;
            promptFadeMode = PromptFadeMode.Out;
            promptFadeElapsed = 0f;
            promptFadeStartAlpha = mutationChoicePrompt.alpha;
        }

        private void AdvanceMutationPrompt(float deltaTime)
        {
            if (promptFadeMode == PromptFadeMode.None || mutationChoicePrompt == null) return;
            promptFadeElapsed += deltaTime;
            if (promptFadeMode == PromptFadeMode.In)
            {
                float delay = Mathf.Max(0f, mutationPromptFadeInDelay);
                float duration = Mathf.Max(0f, mutationPromptFadeInDuration);
                if (promptFadeElapsed < delay) return;
                float progress = duration <= 0f ? 1f : Mathf.Clamp01((promptFadeElapsed - delay) / duration);
                mutationChoicePrompt.alpha = Mathf.SmoothStep(0f, 1f, progress);
                if (progress >= 1f) promptFadeMode = PromptFadeMode.None;
                return;
            }

            float fadeOutDuration = Mathf.Max(0f, mutationPromptFadeOutDuration);
            float fadeOutProgress = fadeOutDuration <= 0f ? 1f : Mathf.Clamp01(promptFadeElapsed / fadeOutDuration);
            mutationChoicePrompt.alpha = Mathf.Lerp(promptFadeStartAlpha, 0f, Mathf.SmoothStep(0f, 1f, fadeOutProgress));
            if (fadeOutProgress >= 1f) HideMutationPrompt();
        }

        private void HideMutationPrompt()
        {
            promptFadeMode = PromptFadeMode.None;
            promptFadeElapsed = 0f;
            promptFadeStartAlpha = 0f;
            if (mutationChoicePrompt == null) return;
            mutationChoicePrompt.alpha = 0f;
            mutationChoicePrompt.interactable = false;
            mutationChoicePrompt.blocksRaycasts = false;
            mutationChoicePrompt.gameObject.SetActive(false);
        }

        private void OnDisable() { HideMutationPrompt(); CancelMutationVFX(); ResetPresentation(); UnsubscribeChoices(); }
    }
}
