using UnityEngine;

[DisallowMultipleComponent]
public sealed class TreasureKey : MonoBehaviour
{
    [Range(0, 3)] public int keyId;
    public Transform visual;
    public ParticleSystem collectSparkles;
    [Min(0f)] public float spinSpeed = 100f;
    private bool collected;

    private void Update()
    {
        if (!collected && visual != null) visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        Collider keyCollider = GetComponent<Collider>();
        Debug.Log("[KEY-DIAG] trigger-entry keyId=" + keyId + " otherCollider=" + other.name + " keyCollider=" + (keyCollider != null ? keyCollider.name : "none") + " playerDetected=" + (player != null) + " playerPosition=" + (player != null ? player.transform.position.ToString() : "n/a") + " keyPosition=" + transform.position, this);
        if (collected)
        {
            Debug.Log("[KEY-DIAG] collection-rejected keyId=" + keyId + " reason=already-collected", this);
            return;
        }
        if (player == null)
        {
            Debug.Log("[KEY-DIAG] collection-rejected keyId=" + keyId + " reason=collider-has-no-PlayerMovement", this);
            return;
        }
        if (GameManager.Instance == null)
        {
            Debug.Log("[KEY-DIAG] collection-rejected keyId=" + keyId + " reason=GameManager-missing", this);
            return;
        }
        bool accepted = GameManager.Instance.CollectTreasureKey(keyId);
        if (accepted) AudioManager.Play("sfx_key");
        Debug.Log("[KEY-DIAG] collection-result keyId=" + keyId + " accepted=" + accepted, this);
        if (!accepted) return;
        collected = true;
        Collider trigger = GetComponent<Collider>();
        if (trigger != null) trigger.enabled = false;
        if (visual != null) visual.gameObject.SetActive(false);
        if (collectSparkles != null)
        {
            collectSparkles.transform.SetParent(null, true);
            collectSparkles.Play();
            Destroy(collectSparkles.gameObject, 2f);
        }
        Destroy(gameObject);
    }
}
