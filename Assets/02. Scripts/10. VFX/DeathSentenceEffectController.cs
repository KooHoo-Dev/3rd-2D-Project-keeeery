using UnityEngine;

namespace Arcanist
{
    public class DeathSentenceEffectController : MonoBehaviour
    {
        [SerializeField] private ScreenEffectController screenEffectController;

        private void OnEnable()
        {
            ResolveScreenEffectController();
            screenEffectController?.PlayDeathSentenceFlash();
        }

        private void ResolveScreenEffectController()
        {
            if (screenEffectController == null)
                screenEffectController = FindFirstObjectByType<ScreenEffectController>();
        }
    }
}