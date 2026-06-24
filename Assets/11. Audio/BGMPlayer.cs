using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class BGMPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private bool stopCurrentBGM;
        [SerializeField] private float playDelay;

        private IEnumerator Start()
        {
            if (playDelay > 0.0f)
                yield return new WaitForSeconds(playDelay);

            if (stopCurrentBGM)
            {
                AudioManager.Instance.StopBGM();
                yield break;
            }

            AudioManager.Instance.PlayBGM(bgmClip);
        }
    }
}