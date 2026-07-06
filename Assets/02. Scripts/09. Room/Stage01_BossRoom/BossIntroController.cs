using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace Arcanist
{
    public class BossIntroController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private CinemachineCamera bossCamera;
        [SerializeField] private float defaultSize = 5.0f;
        [SerializeField] private float zoomSize = 3.0f;

        [Header("Timing")]
        [SerializeField] private float startDelay = 0.5f;
        [SerializeField] private float zoomInDuration = 0.8f;
        [SerializeField] private float holdDuration = 1.0f;
        [SerializeField] private float zoomOutDuration = 0.8f;

        [Header("Boss")]
        [SerializeField] private BossController bossController;
        [SerializeField] private Animator bossAnimator;
        [SerializeField] private string introAnimationName = "Boss_Golem_Idle";

        [Header("Control Lock")]
        [SerializeField] private MonoBehaviour[] disableDuringIntro;

        private void Awake()
        {
            PrepareBossForIntro();
        }

        private void Start()
        {
            StartCoroutine(IntroCoroutine());
        }

        private IEnumerator IntroCoroutine()
        {
            SetControlEnabled(false);
            PrepareBossForIntro();
            SetCameraSize(defaultSize);

            yield return new WaitForSeconds(startDelay);

            if (bossAnimator != null && string.IsNullOrEmpty(introAnimationName) == false)
                bossAnimator.Play(introAnimationName);

            yield return LerpCameraSize(defaultSize, zoomSize, zoomInDuration);
            yield return new WaitForSeconds(holdDuration);
            yield return LerpCameraSize(zoomSize, defaultSize, zoomOutDuration);

            if (bossController != null)
                bossController.StartBoss();

            SetControlEnabled(true);
        }

        private void PrepareBossForIntro()
        {
            if (bossController == null)
                return;

            bossController.SetStartOnStart(false);
            bossController.StopBoss();
        }

        private IEnumerator LerpCameraSize(float from, float to, float duration)
        {
            float timer = 0.0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                float easedT = t * t * (3.0f - 2.0f * t);

                SetCameraSize(Mathf.Lerp(from, to, easedT));

                yield return null;
            }

            SetCameraSize(to);
        }

        private void SetCameraSize(float size)
        {
            if (bossCamera == null)
                return;

            bossCamera.Lens.OrthographicSize = size;
        }

        private void SetControlEnabled(bool isEnabled)
        {
            for (int i = 0; i < disableDuringIntro.Length; i++)
            {
                if (disableDuringIntro[i] != null)
                    disableDuringIntro[i].enabled = isEnabled;
            }
        }
    }
}