using UnityEngine;
using TMPro;
using System.Collections;

namespace Utilities
{
    public class FloatingText : MonoBehaviour
    {
        [field: SerializeField] public float VerticalSpeed { get; private set; } = 1.0f;
        [field: SerializeField] public float LifeTime { get; private set; } = 2.0f;

        private TMP_Text text;
        private Renderer textRenderer;
        private Color originColor;
        private float progressTime = 0.0f;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            textRenderer = GetComponent<Renderer>();
            
            textRenderer.sortingOrder = 1000;
            originColor = text.color;
        }

        public void Show(string message, Color color, Vector3 worldPosition)
        {
            text.SetText(message);
            originColor = color;
            transform.position = worldPosition;
            StartCoroutine(AnimateCoroutine());
        }

        private IEnumerator AnimateCoroutine()
        {
            progressTime = 0.0f;
            Color targetColor = originColor;
            targetColor.a = 0.0f;

            while (true)
            {
                if (progressTime > LifeTime) break;

                Color nowColor = Color.Lerp(
                    originColor, targetColor, progressTime / LifeTime);
                text.color = nowColor;

                transform.Translate(Vector3.up * VerticalSpeed * Time.deltaTime);

                yield return null;
                progressTime += Time.deltaTime;
            }

            gameObject.SetActive(false);
        }
    }

}
