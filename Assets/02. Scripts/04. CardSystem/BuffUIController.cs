using UnityEngine;
using UnityEngine.UI;

namespace Arcanist
{
    public class BuffUIController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image fillImage;

        private float duration;
        private float startTime;
        private bool isRunning;

        public void Initialize(Sprite icon, float duration)
        {
            if (iconImage != null)
                iconImage.sprite = icon;

            this.duration = Mathf.Max(0.01f, duration);
            startTime = Time.time;
            isRunning = true;

            UpdateFill(0.0f);
            gameObject.SetActive(true);
        }

        public void Refresh(float duration)
        {
            this.duration = Mathf.Max(0.01f, duration);
            startTime = Time.time;
            isRunning = true;

            UpdateFill(0.0f);
        }

        public void Hide()
        {
            isRunning = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isRunning == false)
                return;

            float elapsedTime = Time.time - startTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / duration);

            UpdateFill(normalizedTime);

            if (normalizedTime >= 1.0f)
                Hide();
        }

        private void UpdateFill(float amount)
        {
            if (fillImage == null)
                return;

            fillImage.fillAmount = amount;
        }
    }
}
