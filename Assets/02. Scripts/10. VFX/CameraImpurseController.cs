using UnityEngine;
using Unity.Cinemachine;

namespace Arcanist
{
    public class CameraImpulseController : MonoBehaviour
    {
        [SerializeField] private CinemachineImpulseSource impulseSource;

        public void Play()
        {
            impulseSource.GenerateImpulse();
        }
    }
}