using UnityEngine;

namespace Arcanist
{
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Transform targetCamera;
        [SerializeField, Range(0.0f, 1.0f)] private float parallaxFactor = 0.2f;
        [SerializeField] private bool followY = false;
        [SerializeField] private bool repeatX = true;
        [SerializeField] private float pieceWidth = 20.0f;

        private Vector3 startPosition;
        private Vector3 cameraStartPosition;
        private Transform[] pieces;

        private void Awake()
        {
            if (targetCamera == null && Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }

            startPosition = transform.position;

            if (targetCamera != null)
            {
                cameraStartPosition = targetCamera.position;
            }

            CachePieces();
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                return;
            }

            MoveLayer();

            if (repeatX)
            {
                RepeatPieces();
            }
        }

        private void CachePieces()
        {
            pieces = new Transform[transform.childCount];

            for (int i = 0; i < transform.childCount; i++)
            {
                pieces[i] = transform.GetChild(i);
            }
        }

        private void MoveLayer()
        {
            Vector3 cameraDelta = targetCamera.position - cameraStartPosition;

            float x = startPosition.x + cameraDelta.x * parallaxFactor;
            float y = followY ? startPosition.y + cameraDelta.y * parallaxFactor : startPosition.y;

            transform.position = new Vector3(x, y, startPosition.z);
        }

        private void RepeatPieces()
        {
            if (pieces == null || pieces.Length < 2 || pieceWidth <= 0.0f)
            {
                return;
            }

            float cameraLocalX = targetCamera.position.x - transform.position.x;
            float totalWidth = pieceWidth * pieces.Length;
            float halfWidth = pieceWidth * 0.5f;

            for (int i = 0; i < pieces.Length; i++)
            {
                Vector3 localPosition = pieces[i].localPosition;

                if (localPosition.x + halfWidth < cameraLocalX - totalWidth * 0.5f)
                {
                    localPosition.x += totalWidth;
                    pieces[i].localPosition = localPosition;
                }
                else if (localPosition.x - halfWidth > cameraLocalX + totalWidth * 0.5f)
                {
                    localPosition.x -= totalWidth;
                    pieces[i].localPosition = localPosition;
                }
            }
        }
    }
}