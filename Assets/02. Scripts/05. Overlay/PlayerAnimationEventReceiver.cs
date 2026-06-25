using UnityEngine;

namespace Arcanist
{
    public class PlayerAnimationEventReceiver : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;

        public void ShowDeathWindow()
        {
            playerController.ShowDeathWindow();
        }
    }
}