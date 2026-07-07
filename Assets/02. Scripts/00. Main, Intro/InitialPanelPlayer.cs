using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class InitialPanelPlayer : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string stateName = "StageNameIntro";

        private IEnumerator Start()
        {
            yield return null;
            yield return null;

            animator.Rebind();
            animator.Update(0f);
            animator.Play(stateName, 0, 0f);
        }
    }
}