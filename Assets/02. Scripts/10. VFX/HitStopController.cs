using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class HitStopController : MonoBehaviour
    {
        private Coroutine routine;

        public void Play(float duration)
        {
            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(PlayRoutine(duration));
        }

        private IEnumerator PlayRoutine(float duration)
        {
            Time.timeScale = 0.0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1.0f;

            routine = null;
        }
    }
}