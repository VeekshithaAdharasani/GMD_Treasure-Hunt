using UnityEngine;

[DisallowMultipleComponent]
public sealed class AdventurePortal : MonoBehaviour
{
    public AdventurePortal destination;
    public Transform arrivalPoint;
    [Range(1, 3)] public int destinationLevel = 1;
    private static float nextAllowedTeleport;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTeleportGuard() => nextAllowedTeleport = 0f;

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PlayerMovement>();
        if (player == null || destination == null || arrivalPoint == null ||
            Time.time < nextAllowedTeleport || GameManager.Instance == null || GameManager.Instance.IsInputLocked) return;
        nextAllowedTeleport = Time.time + 0.75f;
        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.SetPositionAndRotation(arrivalPoint.position, arrivalPoint.rotation);
        Physics.SyncTransforms();
        if (controller != null) controller.enabled = true;
        var camera = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (camera != null) camera.SnapToTarget();
        GameManager.Instance.SetAdventureStage(destinationLevel,
            destinationLevel == 1 ? "Forgotten Entrance" : destinationLevel == 2 ? "Ancient Ruins" : "Final Treasure Vault");
    }
}
