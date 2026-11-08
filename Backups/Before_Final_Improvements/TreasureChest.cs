using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class TreasureChest : MonoBehaviour
{
    public Transform lid;
    public ParticleSystem openingSparkles;
    public Light treasureLight;
    [Min(0.1f)] public float openDuration = 1.25f;
    private bool activated;
    private bool playerInRange;
    private Quaternion lidClosedRotation;
    private Quaternion lidOpenRotation;

    private void Awake()
    {
        if (lid != null)
        {
            lidClosedRotation = lid.localRotation;
            lidOpenRotation = lidClosedRotation * Quaternion.Euler(-70f, 0f, 0f);
        }
        if (treasureLight != null) treasureLight.enabled = false;
        if (openingSparkles != null) openingSparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Update()
    {
        if (!playerInRange || activated || GameManager.Instance == null) return;
        if (GameManager.Instance.IsInputLocked) return;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("[KEY-DIAG] chest-E-interaction-request collected=" + GameManager.Instance.TreasureKeys + " required=" + GameManager.Instance.requiredTreasureKeys, this);
            Interact();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        Debug.Log("[KEY-DIAG] chest-range-entry collider=" + other.name + " playerDetected=" + (player != null), this);
        if (player == null || GameManager.Instance == null) return;
        playerInRange = true;
        player.StopRunnerAtChest();
        GameManager.Instance.SetInteractionHint("Press E to open the treasure chest.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerMovement>() == null) return;
        playerInRange = false;
        if (!activated && GameManager.Instance != null) GameManager.Instance.SetInteractionHint(string.Empty);
    }

    private void Interact()
    {
        if (activated || GameManager.Instance == null) return;
        int collected = GameManager.Instance.TreasureKeys;
        int required = GameManager.Instance.requiredTreasureKeys;
        bool unlocked = GameManager.Instance.HasAllTreasureKeys;
        Debug.Log("[KEY-DIAG] chest-interaction collected=" + collected + " required=" + required + " unlockDecision=" + unlocked, this);
        if (!unlocked)
        {
            GameManager.Instance.ShowLockedTreasure();
            return;
        }
        activated = true;
        GameManager.Instance.SetInteractionHint(string.Empty);
        Collider trigger = GetComponent<Collider>();
        if (trigger != null) trigger.enabled = false;
        if (treasureLight != null) treasureLight.enabled = true;
        if (openingSparkles != null) openingSparkles.Play();
        StartCoroutine(OpenAndWin());
    }

    private IEnumerator OpenAndWin()
    {
        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (lid != null) lid.localRotation = Quaternion.Slerp(lidClosedRotation, lidOpenRotation, Mathf.Clamp01(elapsed / openDuration));
            yield return null;
        }
        if (lid != null) lid.localRotation = lidOpenRotation;
        yield return new WaitForSecondsRealtime(0.35f);
        GameManager.Instance?.WinGame();
    }
}
