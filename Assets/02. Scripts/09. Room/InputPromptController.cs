using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Arcanist
{
    public class InputPromptController : MonoBehaviour
    {
        [Header("Prompt Objects")]
        [SerializeField] private GameObject upArrowInputPrompt;
        [SerializeField] private GameObject downArrowInputPrompt;
        [SerializeField] private TMP_Text upArrowPromptText;

        [Header("Prompt Text")]
        [SerializeField] private string rewardPromptText = "보상 선택";
        [SerializeField] private string nextRoomPromptText = "다음으로 이동";

        [Header("Fade")]
        [SerializeField] private float fadeDuration = 0.15f;

        private CanvasGroup upPromptGroup;
        private CanvasGroup downPromptGroup;
        private Tween fadeTween;
        private bool isVisible;
        private bool isDownPromptVisible;
        private bool isQuittingOrDestroyed;

        private void Awake()
        {
            Initialize();
            HideImmediate();
        }

        private void OnDisable()
        {
            KillFadeTween();
        }

        private void OnDestroy()
        {
            isQuittingOrDestroyed = true;
            KillFadeTween();
        }

        public void ShowRewardPrompt()
        {
            SetUpPromptText(rewardPromptText);
            Show(false);
        }

        public void ShowNextRoomPrompt()
        {
            SetUpPromptText(nextRoomPromptText);
            Show(true);
        }

        public void Hide()
        {
            if (!isVisible)
                return;

            isVisible = false;

            if (isQuittingOrDestroyed || !isActiveAndEnabled)
            {
                KillFadeTween();
                SetPromptAlpha(0.0f);
                SetPromptInteractable(false);
                return;
            }

            StartFade(false);
        }

        private void Initialize()
        {
            upPromptGroup = GetOrAddCanvasGroup(upArrowInputPrompt);
            downPromptGroup = GetOrAddCanvasGroup(downArrowInputPrompt);

            if (upArrowPromptText == null && upArrowInputPrompt != null)
                upArrowPromptText = upArrowInputPrompt.GetComponentInChildren<TMP_Text>(true);
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject promptObject)
        {
            if (promptObject == null)
                return null;

            CanvasGroup group = promptObject.GetComponent<CanvasGroup>();
            if (group == null)
                group = promptObject.AddComponent<CanvasGroup>();

            return group;
        }

        private void Show(bool showDownPrompt)
        {
            SetPromptActive(true, showDownPrompt);
            isDownPromptVisible = showDownPrompt;

            if (isVisible)
            {
                SetPromptAlpha(1.0f);
                return;
            }

            isVisible = true;
            StartFade(true);
        }

        private void HideImmediate()
        {
            isVisible = false;
            isDownPromptVisible = false;

            KillFadeTween();

            SetPromptActive(true, true);
            SetPromptAlpha(0.0f);
            SetPromptInteractable(false);
        }

        private void SetUpPromptText(string text)
        {
            if (upArrowPromptText != null)
                upArrowPromptText.text = text;
        }

        private void SetPromptActive(bool showUpPrompt, bool showDownPrompt)
        {
            if (upArrowInputPrompt != null && !upArrowInputPrompt.activeSelf)
                upArrowInputPrompt.SetActive(true);

            if (downArrowInputPrompt != null && !downArrowInputPrompt.activeSelf)
                downArrowInputPrompt.SetActive(true);

            SetCanvasGroupVisible(upPromptGroup, showUpPrompt);
            SetCanvasGroupVisible(downPromptGroup, showDownPrompt);
        }

        private void SetCanvasGroupVisible(CanvasGroup group, bool visible)
        {
            if (group == null)
                return;

            group.interactable = visible;
            group.blocksRaycasts = visible;
        }

        private void SetPromptInteractable(bool interactable)
        {
            SetCanvasGroupVisible(upPromptGroup, interactable);
            SetCanvasGroupVisible(downPromptGroup, interactable && isDownPromptVisible);
        }

        private void StartFade(bool show)
        {
            if (isQuittingOrDestroyed || !isActiveAndEnabled)
            {
                SetPromptAlpha(show ? 1.0f : 0.0f);
                SetPromptInteractable(show);
                return;
            }

            KillFadeTween();
            SetPromptActive(true, show && isDownPromptVisible);
            SetPromptInteractable(show);

            float targetAlpha = show ? 1.0f : 0.0f;
            float startAlpha = upPromptGroup != null ? upPromptGroup.alpha : 0.0f;

            fadeTween = DOTween.To(
                    () => startAlpha,
                    SetPromptAlpha,
                    targetAlpha,
                    Mathf.Max(0.0f, fadeDuration))
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    SetPromptAlpha(targetAlpha);

                    if (!show)
                        SetPromptInteractable(false);

                    fadeTween = null;
                });
        }

        private void KillFadeTween()
        {
            fadeTween?.Kill();
            fadeTween = null;
        }

        private void SetPromptAlpha(float alpha)
        {
            if (upPromptGroup != null)
                upPromptGroup.alpha = alpha;

            if (downPromptGroup != null)
                downPromptGroup.alpha = isDownPromptVisible ? alpha : 0.0f;
        }
    }
}
