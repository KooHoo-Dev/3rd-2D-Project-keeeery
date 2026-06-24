using UnityEngine;

namespace Arcanist
{
    public class PlayerShadowProjector : MonoBehaviour
    {
        [SerializeField] private Transform shadow;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float maxDistance = 5.0f;
        [SerializeField] private float rayStartYOffset = 0.2f;
        [SerializeField] private float shadowXOffset = 0.0f;
        [SerializeField] private bool flipXOffsetWithCharacter = true;
        [SerializeField] private Transform flipReference;
        [SerializeField] private float groundOffset = 0.02f;
        [SerializeField] private float minXScale = 0.45f;
        [SerializeField] private float maxXScale = 1.0f;
        [SerializeField] private float fixedYScale = 1.0f;

        private SpriteRenderer shadowRenderer;
        private PlayerController playerController;
        private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[8];

        private void Awake()
        {
            shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            playerController = GetComponent<PlayerController>();
            shadow.SetParent(null, true);

            // Shadow가 Player 자식이면 부모 이동/스케일 영향을 받기 쉬우므로
            // 월드 오브젝트처럼 분리해서 지형 위치에 고정되게 만든다.
        }

        private void LateUpdate()
        {
            float offsetDirection = GetOffsetDirection();
            Vector2 origin = transform.position + Vector3.right * shadowXOffset * offsetDirection + Vector3.up * rayStartYOffset;
            int hitCount = Physics2D.RaycastNonAlloc(
                origin,
                Vector2.down,
                hitBuffer,
                maxDistance + rayStartYOffset,
                groundLayer);

            RaycastHit2D groundHit = default;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = hitBuffer[i];

                if (hit.collider == null)
                    continue;

                if (hit.collider.transform.IsChildOf(transform))
                    continue;

                groundHit = hit;
                break;
            }

            if (groundHit.collider == null)
            {
                shadow.gameObject.SetActive(false);
                return;
            }

            shadow.gameObject.SetActive(true);

            Vector3 position = groundHit.point;
            position.y += groundOffset;
            position.z = shadow.position.z;
            shadow.position = position;

            float distance = Mathf.Max(0.0f, groundHit.distance - rayStartYOffset);
            float distanceRate = Mathf.Clamp01(distance / maxDistance);
            float xScale = Mathf.Lerp(maxXScale, minXScale, distanceRate);

            shadow.localScale = new Vector3(xScale, fixedYScale, 1.0f);
        }

        private float GetOffsetDirection()
        {
            if (flipXOffsetWithCharacter == false)
                return 1.0f;

            if (playerController != null)
                return playerController.FacingDirection;

            Transform reference = flipReference != null ? flipReference : transform;
            return reference.lossyScale.x < 0.0f ? -1.0f : 1.0f;
        }
    }
}