using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace Arcanist
{
    public class BossDeathController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private CinemachineCamera bossCamera;
        [SerializeField] private float zoomSize = 3.0f;
        [SerializeField] private float zoomInDuration = 0.35f;
        [SerializeField] private float holdDuration = 1.0f;
        [SerializeField] private float zoomOutDuration = 0.6f;

        [Header("Impulse")]
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private float firstImpulseForce = 2.8f;
        [SerializeField] private float secondImpulseForce = 1.8f;
        [SerializeField] private float thirdImpulseForce = 1.0f;
        [SerializeField] private float secondImpulseDelay = 0.08f;
        [SerializeField] private float thirdImpulseDelay = 0.12f;

        [Header("Control Lock")]
        [SerializeField] private MonoBehaviour[] disableDuringDeathCinematic;

        private Coroutine coroutine;
        private float defaultSize;
        private bool hasCachedValues;

        private void Awake()
        {
            CacheValues();
        }

        public void Play()
        {
            if (coroutine != null)
                StopCoroutine(coroutine);

            coroutine = StartCoroutine(Coroutine());
        }

        private IEnumerator Coroutine()
        {
            CacheValues();
            SetControlEnabled(false);

            yield return LerpCameraSize(GetCurrentSize(), zoomSize, zoomInDuration);
            yield return PlayDeathImpulses();
            yield return new WaitForSeconds(holdDuration);
            yield return LerpCameraSize(GetCurrentSize(), defaultSize, zoomOutDuration);

            SetControlEnabled(true);
            coroutine = null;
        }

        private IEnumerator LerpCameraSize(float from, float to, float duration)
        {
            if (bossCamera == null)
                yield break;

            if (duration <= 0.0f)
            {
                SetCameraSize(to);
                yield break;
            }

            float elapsedTime = 0.0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                float easedT = t * t * (3.0f - 2.0f * t);

                SetCameraSize(Mathf.Lerp(from, to, easedT));
                yield return null;
            }

            SetCameraSize(to);
        }

        private void CacheValues()
        {
            if (hasCachedValues)
                return;

            defaultSize = GetCurrentSize();

            if (impulseSource == null)
                impulseSource = GetComponent<CinemachineImpulseSource>();

            hasCachedValues = true;
        }

        private float GetCurrentSize()
        {
            return bossCamera != null ? bossCamera.Lens.OrthographicSize : 0.0f;
        }

        private void SetCameraSize(float size)
        {
            if (bossCamera == null)
                return;

            bossCamera.Lens.OrthographicSize = size;
        }

        private IEnumerator PlayDeathImpulses()
        {
            PlayImpulse(firstImpulseForce);

            yield return new WaitForSeconds(secondImpulseDelay);
            PlayImpulse(secondImpulseForce);

            yield return new WaitForSeconds(thirdImpulseDelay);
            PlayImpulse(thirdImpulseForce);
        }

        private void PlayImpulse(float force)
        {
            if (impulseSource == null)
                return;

            impulseSource.GenerateImpulse(force);
        }

        private void SetControlEnabled(bool isEnabled)
        {
            for (int i = 0; i < disableDuringDeathCinematic.Length; i++)
            {
                if (disableDuringDeathCinematic[i] != null)
                    disableDuringDeathCinematic[i].enabled = isEnabled;
            }
        }
    }
}
