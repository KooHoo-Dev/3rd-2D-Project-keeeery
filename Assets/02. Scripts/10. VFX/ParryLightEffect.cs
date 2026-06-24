using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Arcanist
{
    public class ParryLightEffect : MonoBehaviour
    {
        [SerializeField] private Light2D parryLight;
        [SerializeField] private ParticleSystem parryParticle;

        [Header("Gauge FX")]
        [SerializeField] private ParticleSystem parryGaugeFxPrefab;
        [SerializeField] private Transform parryGaugeFxStart;
        [SerializeField] private Transform parryGaugeFxTarget;
        [SerializeField] private Transform parryGaugeFxParent;
        [SerializeField] private float parryGaugeFxDestroyDelay = 0.6f;

        [SerializeField] private HitStopController hitStopController;
        [SerializeField] private CameraImpulseController cameraImpulseController;
        [SerializeField] private float cameraShakeDelay = 0.07f;

        [SerializeField] private float hitStopDuration = 0.05f;

        [Header("Default")]
        [SerializeField] private float defaultIntensity = 0.0f;
        [SerializeField] private float defaultRadius = 3.0f;

        [Header("Parry Burst")]
        [SerializeField] private float burstIntensity = 1.8f;
        [SerializeField] private float burstRadius = 7.0f;
        [SerializeField] private float burstInTime = 0.04f;
        [SerializeField] private float burstOutTime = 0.18f;

        private Coroutine effectRoutine;

        public void Play()
        {
            if (effectRoutine != null)
                StopCoroutine(effectRoutine);

            if (parryParticle != null)
                parryParticle.Play();

            PlayParryGaugeFx();

            if (hitStopController != null)
                hitStopController.Play(hitStopDuration);

            if (cameraImpulseController != null)
                StartCoroutine(PlayCameraShakeDelay());

            effectRoutine = StartCoroutine(PlayRoutine());
        }

        private void PlayParryGaugeFx()
        {
            if (parryGaugeFxPrefab == null || parryGaugeFxStart == null)
                return;

            Transform parent = parryGaugeFxParent != null ? parryGaugeFxParent : parryGaugeFxStart.parent;
            ParticleSystem gaugeFx = Instantiate(parryGaugeFxPrefab, parryGaugeFxStart.position, Quaternion.identity, parent);

            Vector3 direction = parryGaugeFxTarget.position - parryGaugeFxStart.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            gaugeFx.transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle - 20.0f);
            gaugeFx.Play();

            Destroy(gaugeFx.gameObject, parryGaugeFxDestroyDelay);
        }
        
        private IEnumerator PlayRoutine()
        {
            yield return LerpLight(defaultIntensity, burstIntensity, defaultRadius, burstRadius, burstInTime);
            yield return LerpLight(burstIntensity, defaultIntensity, burstRadius, defaultRadius, burstOutTime);

            parryLight.intensity = defaultIntensity;
            parryLight.pointLightOuterRadius = defaultRadius;
            effectRoutine = null;
        }
        
        private IEnumerator PlayCameraShakeDelay()
        {
            yield return new WaitForSecondsRealtime(cameraShakeDelay);
            cameraImpulseController.Play();
        }

        private IEnumerator LerpLight(float fromIntensity, float toIntensity, float fromRadius, float toRadius, float duration)
        {
            float timer = 0.0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);

                parryLight.intensity = Mathf.Lerp(fromIntensity, toIntensity, t);
                parryLight.pointLightOuterRadius = Mathf.Lerp(fromRadius, toRadius, t);

                yield return null;
            }
        }
    }
}