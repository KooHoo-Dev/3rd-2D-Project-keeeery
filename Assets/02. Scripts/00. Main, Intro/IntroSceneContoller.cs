using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Utilities;

namespace Arcanist
{
    public class IntroSceneController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image cutsceneImage;
        [SerializeField] private CanvasGroup fadePanel;
        [SerializeField] private TMP_Text captionText;
        [SerializeField] private GameObject continuePrompt;
        [SerializeField] private CanvasGroup continuePromptCanvasGroup;
        [SerializeField] private GameObject skipPrompt;
        [SerializeField] private CanvasGroup skipPromptCanvasGroup;

        [Header("Cutscene")]
        [SerializeField] private Sprite[] cutsceneSprites;
        [TextArea]
        [SerializeField] private string[] captions;

        [Header("Timing")]
        [SerializeField] private float fadeDuration = 0.8f;
        [SerializeField] private float displayDuration = 2.5f;
        [SerializeField] private float introBlackScreenDuration = 0.75f;

        [Header("Scene")]
        [SerializeField] private string nextSceneName = "Stage01_Room01";

        private Coroutine cutsceneRoutine;
        private bool isSkipping;
        private bool isWaitingForContinue;

        private void Start()
        {
            continuePrompt.SetActive(false);
            continuePromptCanvasGroup.alpha = 0f;

            fadePanel.alpha = 1f;

            skipPrompt.SetActive(true);
            skipPromptCanvasGroup.alpha = 1f;
            
            cutsceneRoutine = StartCoroutine(PlayCutscene());
        }

        private void Update()
        {
            if (isWaitingForContinue)
            {
                if (SimpleInput.GetKeyDown(Key.UpArrow))
                {
                    isWaitingForContinue = false;
                    StartCoroutine(FadeOutAndLoadNextScene());
                }

                return;
            }

            if (SimpleInput.GetKeyDown(Key.Escape))
                SkipToLastCut();
        }

        private IEnumerator PlayCutscene()
        {
            if (cutsceneSprites.Length == 0)
            {
                LoadNextScene();
                yield break;
            }

            for (int i = 0; i < cutsceneSprites.Length; i++)
            {
                bool isLastCut = i == cutsceneSprites.Length - 1;

                cutsceneImage.sprite = cutsceneSprites[i];
                continuePrompt.SetActive(false);
                continuePromptCanvasGroup.alpha = 0f;

                captionText.gameObject.SetActive(!isLastCut);
                captionText.text = !isLastCut && i < captions.Length ? captions[i] : string.Empty;

                if (i == 0) yield return new WaitForSeconds(introBlackScreenDuration);

                yield return Fade(1f, 0f);

                if (isLastCut)
                {
                    yield return HideSkipPrompt();
                    yield return ShowContinuePrompt();
                    isWaitingForContinue = true;
                    yield break;
                }

                yield return new WaitForSeconds(displayDuration);
                yield return Fade(0f, 1f);
            }
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            fadePanel.alpha = from;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                yield return null;
            }

            fadePanel.alpha = to;
        }

        private IEnumerator HideSkipPrompt()
        {
            float duration = 0.5f;
            float elapsed = 0f;
            float startAlpha = skipPromptCanvasGroup.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                skipPromptCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }

            skipPromptCanvasGroup.alpha = 0f;
            skipPrompt.SetActive(false);
        }

        private IEnumerator ShowContinuePrompt()
        {
            continuePrompt.SetActive(true);

            continuePromptCanvasGroup.alpha = 0f;

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                continuePromptCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            continuePromptCanvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOutAndLoadNextScene()
        {
            continuePrompt.SetActive(false);
            continuePromptCanvasGroup.alpha = 0f;

            LoadNextScene();
            yield break;
        }

        private void SkipToLastCut()
        {
            if (isSkipping) return;

            isSkipping = true;
            isWaitingForContinue = false;

            if (cutsceneRoutine != null)
            {
                StopCoroutine(cutsceneRoutine);
                cutsceneRoutine = null;
            }

            if (cutsceneSprites.Length == 0)
            {
                LoadNextScene();
                return;
            }

            int lastIndex = cutsceneSprites.Length - 1;

            cutsceneImage.sprite = cutsceneSprites[lastIndex];
            fadePanel.alpha = 0f;

            captionText.gameObject.SetActive(false);

            continuePrompt.SetActive(false);
            continuePromptCanvasGroup.alpha = 0f;

            StartCoroutine(ShowContinuePromptThenWait());
        }

        private IEnumerator ShowContinuePromptThenWait()
        {
            yield return HideSkipPrompt();
            yield return ShowContinuePrompt();
            isWaitingForContinue = true;
        }

        private void LoadNextScene()
        {
            ScreenFadeManager.Instance.LoadSceneWithFade(nextSceneName, fadeDuration);
        }
    }
}