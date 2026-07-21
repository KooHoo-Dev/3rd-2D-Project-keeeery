using DG.Tweening;
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

        private Sequence effectSequence;
        private Tween cameraShakeTween;

        private void OnDisable()
        {
            KillTweens();
            ResetLight();
        }

        public void Play()
        {
            KillTweens();

            if (parryParticle != null)
                parryParticle.Play();

            PlayParryGaugeFx();

            if (hitStopController != null)
                hitStopController.Play(hitStopDuration);

            if (cameraImpulseController != null)
            {
                cameraShakeTween = DOVirtual.DelayedCall(
                        Mathf.Max(0.0f, cameraShakeDelay),
                        cameraImpulseController.Play,
                        true)
                    .OnComplete(() => cameraShakeTween = null);
            }

            PlayLightSequence();
        }

        private void PlayParryGaugeFx()
        {
            if (parryGaugeFxPrefab == null || parryGaugeFxStart == null || parryGaugeFxTarget == null)
                return;

            Transform parent = parryGaugeFxParent != null ? parryGaugeFxParent : parryGaugeFxStart.parent;
            ParticleSystem gaugeFx = Instantiate(parryGaugeFxPrefab, parryGaugeFxStart.position, Quaternion.identity, parent);

            Vector3 direction = parryGaugeFxTarget.position - parryGaugeFxStart.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            gaugeFx.transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle - 20.0f);
            gaugeFx.Play();

            Destroy(gaugeFx.gameObject, parryGaugeFxDestroyDelay);
        }
        
        private void PlayLightSequence()
        {
            if (parryLight == null)
                return;

            ResetLight();
            effectSequence = DOTween.Sequence().SetUpdate(true);
            effectSequence.Append(CreateLightTween(burstIntensity, burstRadius, burstInTime));
            effectSequence.Append(CreateLightTween(defaultIntensity, defaultRadius, burstOutTime));
            effectSequence.OnComplete(() =>
            {
                effectSequence = null;
                ResetLight();
            });
        }

        private Tween CreateLightTween(float intensity, float radius, float duration)
        {
            float safeDuration = Mathf.Max(0.01f, duration);
            return DOTween.Sequence()
                .Join(DOTween.To(
                    () => parryLight.intensity,
                    value => parryLight.intensity = value,
                    intensity,
                    safeDuration))
                .Join(DOTween.To(
                    () => parryLight.pointLightOuterRadius,
                    value => parryLight.pointLightOuterRadius = value,
                    radius,
                    safeDuration));
        }

        private void KillTweens()
        {
            effectSequence?.Kill();
            cameraShakeTween?.Kill();
            effectSequence = null;
            cameraShakeTween = null;
        }

        private void ResetLight()
        {
            if (parryLight == null)
                return;

            parryLight.intensity = defaultIntensity;
            parryLight.pointLightOuterRadius = defaultRadius;
        }
    }
}
