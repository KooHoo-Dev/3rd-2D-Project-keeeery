using DG.Tweening;
using UnityEngine;
using TMPro;

namespace Utilities
{
    public class FloatingText : MonoBehaviour
    {
        [field: SerializeField] public float VerticalSpeed { get; private set; } = 1.0f;
        [field: SerializeField] public float LifeTime { get; private set; } = 2.0f;

        private TMP_Text text;
        private Renderer textRenderer;
        private Sequence animationSequence;

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
            textRenderer = GetComponent<Renderer>();
            
            textRenderer.sortingOrder = 1000;
        }

        private void OnDisable()
        {
            KillAnimation();
        }

        public void Show(string message, Color color, Vector3 worldPosition)
        {
            text.SetText(message);
            KillAnimation();
            text.color = color;
            transform.position = worldPosition;
            float duration = Mathf.Max(0.01f, LifeTime);

            animationSequence = DOTween.Sequence()
                .Join(transform.DOMoveY(worldPosition.y + VerticalSpeed * duration, duration).SetEase(Ease.Linear))
                .Join(text.DOFade(0.0f, duration).SetEase(Ease.Linear))
                .OnComplete(() =>
                {
                    animationSequence = null;
                    gameObject.SetActive(false);
                });
        }

        private void KillAnimation()
        {
            animationSequence?.Kill();
            animationSequence = null;
        }
    }

}
