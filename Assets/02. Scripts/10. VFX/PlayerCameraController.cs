using UnityEngine;
using Unity.Cinemachine;

namespace Arcanist
{
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private Rigidbody2D playerRigidbody;

        [Header("Look Ahead")]
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private float lookAheadSmooth = 5f;
        [SerializeField] private bool invertLookAheadDirection = true;

        [Header("Zoom")]
        [SerializeField] private float idleZoomSize = 4.5f;
        [SerializeField] private float moveZoomSize = 5.2f;
        [SerializeField] private float zoomSmooth = 3f;
        [SerializeField] private float idleZoomDelayDistance = 0.15f;

        [Header("Movement Check")]
        [SerializeField] private float moveThreshold = 0.01f;

        private float lastDirection = 1f;
        private Vector3 previousPlayerPosition;

        private void Start()
        {
            previousPlayerPosition = player.position;
        }

        private void LateUpdate()
        {
            float velocityX = (player.position.x - previousPlayerPosition.x) / Time.deltaTime;

            if (Mathf.Abs(velocityX) > moveThreshold)
                lastDirection = Mathf.Sign(velocityX);

            bool isMoving = Mathf.Abs(velocityX) > moveThreshold;

            Vector3 targetPosition = player.position;

            if (isMoving)
            {
                float lookAheadDirection = invertLookAheadDirection ? -lastDirection : lastDirection;
                targetPosition.x += lookAheadDirection * lookAheadDistance;
            }

            cameraTarget.position = Vector3.Lerp(
                cameraTarget.position,
                targetPosition,
                lookAheadSmooth * Time.deltaTime
            );

            float distanceToPlayerX = Mathf.Abs(cameraTarget.position.x - player.position.x);
            bool canIdleZoomIn = isMoving || distanceToPlayerX <= idleZoomDelayDistance;
            float targetZoom = canIdleZoomIn ? (isMoving ? moveZoomSize : idleZoomSize) : moveZoomSize;

            cinemachineCamera.Lens.OrthographicSize = Mathf.Lerp(
                cinemachineCamera.Lens.OrthographicSize,
                targetZoom,
                zoomSmooth * Time.deltaTime
            );

            previousPlayerPosition = player.position;
        }
    }
}