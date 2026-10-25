using UnityEngine;

namespace SnowyWinter.Camera
{
    /// <summary>Keeps a 2D camera centred on a target while preserving its render depth.</summary>
    [DisallowMultipleComponent]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector2 offset;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public Vector2 Offset
        {
            get => offset;
            set => offset = value;
        }

        private void LateUpdate()
        {
            FollowTarget();
        }

        /// <summary>Updates the camera position immediately. Safe to call from cutscenes or tests.</summary>
        public void FollowTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = CalculatePosition(target.position, transform.position.z, offset);
        }

        public static Vector3 CalculatePosition(Vector3 targetPosition, float cameraDepth, Vector2 offset)
        {
            return new Vector3(targetPosition.x + offset.x, targetPosition.y + offset.y, cameraDepth);
        }
    }
}
