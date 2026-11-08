using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Position Follow Settings")]
    [Tooltip("Fixed camera offset relative to player (X=lateral, Y=height, Z=distance behind).")]
    public Vector3 offset = new Vector3(0f, 4.0f, -6.5f);

    [Tooltip("How smoothly camera follows player position.")]
    [Min(0.1f)] public float followSmoothness = 6.0f;

    [Header("Fixed Rotation")]
    [Tooltip("Fixed camera viewing angle. Never rotates during gameplay.")]
    public Vector3 fixedEulerRotation = new Vector3(20f, 0f, 0f);

    [Header("Basic Wall Protection")]
    public bool enableWallProtection = true;
    [Min(0f)] public float targetHeight = 1.4f;
    [Min(0.05f)] public float collisionRadius = 0.25f;
    [Min(0.05f)] public float collisionPadding = 0.35f;
    public LayerMask collisionLayers = ~0;

    private Quaternion fixedRotation;

    private void Awake()
    {
        if (target == null)
        {
            PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
                target = player.transform;
        }

        // Initialize and freeze the fixed viewing direction
        fixedRotation = Quaternion.Euler(fixedEulerRotation);
        transform.rotation = fixedRotation;

        if (target != null)
        {
            SnapToTarget();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Position: follow player position smoothly using fixed offset
        Vector3 desiredPosition = target.position + offset;
        Vector3 finalPosition = desiredPosition;

        // Basic wall obstruction check: Ray/Sphere cast from player to desired position
        if (enableWallProtection)
        {
            Vector3 targetPivot = target.position + Vector3.up * targetHeight;
            Vector3 castDirection = desiredPosition - targetPivot;
            float totalDist = castDirection.magnitude;

            if (totalDist > 0.1f)
            {
                castDirection.Normalize();
                float startOffset = 0.45f;
                Vector3 castStart = targetPivot + castDirection * startOffset;
                float checkDist = Mathf.Max(0.1f, totalDist - startOffset);

                float closestHit = float.PositiveInfinity;
                if (Physics.SphereCast(castStart, collisionRadius, castDirection, out RaycastHit hit, checkDist, collisionLayers.value, QueryTriggerInteraction.Ignore) &&
                    hit.collider != null &&
                    hit.collider.transform.root != target.root &&
                    hit.collider.GetComponentInParent<PlayerMovement>() == null)
                {
                    closestHit = hit.distance;
                }
                else
                {
                    closestHit = float.PositiveInfinity;
                }

                if (!float.IsPositiveInfinity(closestHit))
                {
                    float safeDist = Mathf.Max(0.2f, (closestHit + startOffset) - collisionPadding);
                    finalPosition = targetPivot + castDirection * safeDist;
                }
            }
        }

        // Smoothly follow position
        transform.position = Vector3.Lerp(
            transform.position,
            finalPosition,
            1f - Mathf.Exp(-followSmoothness * Time.deltaTime));

        // Enforce strictly fixed viewing rotation throughout gameplay
        transform.rotation = fixedRotation;
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        fixedRotation = Quaternion.Euler(fixedEulerRotation);
        transform.position = target.position + offset;
        transform.rotation = fixedRotation;
    }
}
