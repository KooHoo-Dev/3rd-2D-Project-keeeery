using UnityEngine;

namespace Arcanist
{
    public class PlayerAnimationEventReceiver : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;

        [Header("Card")]
        [SerializeField] private CardSystem cardSystem;

        public void ShowDeathWindow()
        {
            playerController.ShowDeathWindow();
        }

        public void ExecutePendingCardSkill()
        {
            if (cardSystem == null) return;

            cardSystem.ExecutePendingCardSkill();
        }

        public void EndCardSkill()
        {
            if (playerController == null)
                return;

            playerController.EndCardSkill();
        }
    }
}