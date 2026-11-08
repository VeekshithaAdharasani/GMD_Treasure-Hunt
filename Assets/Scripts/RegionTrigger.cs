using UnityEngine;

/// <summary>
/// RegionTrigger — Attach to any BoxCollider (IsTrigger = true) to announce
/// a region transition to AdventureLevelManager when the player walks through it.
/// The world-builder Editor script places these at the entrance of each section.
/// </summary>
[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public sealed class RegionTrigger : MonoBehaviour
{
    [Tooltip("Which region this trigger announces.")]
    public AdventureLevelManager.Region targetRegion = AdventureLevelManager.Region.ForgottenEntrance;

    private void Awake()
    {
        // Ensure collider is set to trigger
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (AdventureLevelManager.Instance == null) return;
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        AdventureLevelManager.Instance.SetRegion(targetRegion);
    }
}
