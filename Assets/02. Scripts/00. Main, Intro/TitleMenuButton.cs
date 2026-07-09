using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Arcanist
{
    public class TitleMenuButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler
    {
        public enum ButtonType
        {
            NewGame, Continue, Option, Quit
        }

        [SerializeField] private ButtonType buttonType;
        [SerializeField] private float hoverScaleMultiplier = 1.1f;
        [SerializeField] private MainMenuOverlayController overlayController;

        private Vector3 originalScale;
        private TMP_Text text;
        private Color originalColor;
        private bool isInteractable = true;

        private void Awake()
        {
            originalScale = transform.localScale;

            text = GetComponent<TMP_Text>();

            if (text != null)
                originalColor = text.color;

            if (buttonType == ButtonType.Continue)
                RefreshContinueState();
        }

        private void RefreshContinueState()
        {
            isInteractable = SaveManager.Instance.HasCheckpoint();

            if (text == null)
                return;

            text.color = isInteractable
                ? originalColor
                : new Color(0.6f, 0.6f, 0.6f, originalColor.a);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable)
                return;
            transform.localScale = originalScale * hoverScaleMultiplier;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isInteractable)
                return;
            transform.localScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInteractable)
                return;
            if (text != null)
                text.color = new Color(0.7f, 0.7f, 0.7f, originalColor.a);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isInteractable)
                return;
            if (text != null)
                text.color = originalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable)
                return;
            switch (buttonType)
            {
                case ButtonType.NewGame:
                    ScreenFadeManager.Instance.LoadSceneWithFade("Intro", 0.35f);
                    break;

                case ButtonType.Continue:
                    if (!SaveManager.Instance.TryGetCheckpoint(out CheckpointSaveData checkpointData))
                    {
                        Debug.LogWarning("[Title] Continue failed. No checkpoint data.");
                        RefreshContinueState();
                        break;
                    }

                    if (string.IsNullOrEmpty(checkpointData.sceneName))
                    {
                        Debug.LogWarning("[Title] Continue failed. Saved scene name is empty.");
                        break;
                    }

                    ScreenFadeManager.Instance.LoadSceneWithFade(checkpointData.sceneName, 0.35f);
                    break;

                case ButtonType.Option:
                    if (overlayController != null)
                        overlayController.OpenOption();
                    else
                        Debug.LogWarning("[Title] MainMenuOverlayController is not assigned.");
                    break;

                case ButtonType.Quit:
    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
    #else
                    Application.Quit();
    #endif
                    break;
            }
        }
    }
}