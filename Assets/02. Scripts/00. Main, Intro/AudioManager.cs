using System.Collections;
using UnityEngine;
using Utilities;

namespace Arcanist
{
    public class AudioManager : SingletonBase<AudioManager>
    {
        private const string MASTER_VOLUME_KEY = "Option.MasterVolume";

        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private float fadeDuration = 1.0f;

        private Coroutine bgmRoutine;

        public float MasterVolume => AudioListener.volume;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            float savedVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1.0f);
            ApplyMasterVolume(savedVolume);
        }

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        public void SetMasterVolume(float value)
        {
            float clampedValue = Mathf.Clamp01(value);
            ApplyMasterVolume(clampedValue);

            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, clampedValue);
            PlayerPrefs.Save();
        }

        private void ApplyMasterVolume(float value)
        {
            AudioListener.volume = value;
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null)
            {
                StopBGM();
                return;
            }

            if (bgmSource.clip == clip) return;

            if (bgmSource.clip == null)
            {
                bgmSource.clip = clip;
                bgmSource.volume = 1.0f;
                bgmSource.Play();
                return;
            }

            if (bgmRoutine != null)
                StopCoroutine(bgmRoutine);

            bgmRoutine = StartCoroutine(ChangeBGM(clip));
        }

        public void StopBGM()
        {
            if (bgmSource.clip == null) return;

            if (bgmRoutine != null)
                StopCoroutine(bgmRoutine);

            bgmRoutine = StartCoroutine(FadeOutBGM());
        }

        private IEnumerator ChangeBGM(AudioClip clip)
        {
            float startVolume = bgmSource.volume;

            while (bgmSource.volume > 0.0f)
            {
                bgmSource.volume -= Time.deltaTime / fadeDuration;
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.Play();

            while (bgmSource.volume < startVolume)
            {
                bgmSource.volume += Time.deltaTime / fadeDuration;
                yield return null;
            }

            bgmSource.volume = startVolume;
        }

        private IEnumerator FadeOutBGM()
        {
            while (bgmSource.volume > 0.0f)
            {
                bgmSource.volume -= Time.deltaTime / fadeDuration;
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.clip = null;
            bgmSource.volume = 1.0f;
        }
    }
}
