using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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

        private Vector3 originalScale;
        private TMP_Text text;
        private Color originalColor;

        private void Awake()
        {
            originalScale = transform.localScale;

            text = GetComponent<TMP_Text>();

            if (text != null)
                originalColor = text.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = originalScale * hoverScaleMultiplier;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (text != null)
                text.color = new Color(0.7f, 0.7f, 0.7f, originalColor.a);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (text != null)
                text.color = originalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (buttonType)
            {
                case ButtonType.NewGame:
                    SceneManager.LoadScene("Intro");
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