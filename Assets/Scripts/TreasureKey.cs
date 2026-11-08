using UnityEngine;

[DisallowMultipleComponent]
public sealed class TreasureKey : MonoBehaviour
{
    [Range(0, 3)] public int keyId;
    public Transform visual;
    public ParticleSystem collectSparkles;
    public Light keyGlow;
    [Min(0f)] public float spinSpeed = 100f;
    private bool collected;
    private bool passageDiagnosticLogged;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        if (visual == null)
        {
            Transform candidate = transform.Find("Golden Key Visual");
            if (candidate == null)
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                    if (renderer != null) { candidate = renderer.transform; break; }
            visual = candidate;
        }

        if (visual != null)
        {
            visual.gameObject.SetActive(true);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                renderer.enabled = true;
                Material material = renderer.sharedMaterial;
                if (material == null) continue;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Color gold = new Color(1f, 0.72f, 0.12f, 1f);
                if (material.HasProperty(BaseColorId)) block.SetColor(BaseColorId, gold);
                if (material.HasProperty(ColorId)) block.SetColor(ColorId, gold);
                if (material.HasProperty(EmissionColorId)) block.SetColor(EmissionColorId, new Color(2.5f, 1.25f, 0.12f, 1f));
                renderer.SetPropertyBlock(block);
            }
        }

        if (keyGlow == null) keyGlow = GetComponentInChildren<Light>(true);
        if (keyGlow == null)
        {
            GameObject glow = new GameObject("GOLDEN_KEY_GLOW");
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = Vector3.up * 1.4f;
            keyGlow = glow.AddComponent<Light>();
        }
        keyGlow.type = LightType.Point;
        keyGlow.color = new Color(1f, 0.68f, 0.12f);
        keyGlow.intensity = 2.8f;
        keyGlow.range = 5.5f;
        keyGlow.shadows = LightShadows.None;

        if (collectSparkles == null)
        {
            GameObject effect = new GameObject("KEY_PICKUP_SPARKLES");
            effect.transform.SetParent(transform, false);
            collectSparkles = effect.AddComponent<ParticleSystem>();
            // Adding a ParticleSystem can start it immediately. Stop and clear it
            // before changing MainModule.duration to avoid Unity's live-system warning.
            collectSparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = collectSparkles.main;
            main.duration = 0.65f;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.65f;
            main.startSpeed = 2.6f;
            main.startSize = 0.22f;
            main.startColor = new Color(1f, 0.78f, 0.18f, 1f);
            main.maxParticles = 40;
            ParticleSystem.EmissionModule emission = collectSparkles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 32) });
            ParticleSystem.ShapeModule shape = collectSparkles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.16f;
            ParticleSystemRenderer particleRenderer = effect.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null) particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        collectSparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        collectSparkles.transform.localScale = Vector3.one * 1.35f;
        SphereCollider trigger = GetComponent<SphereCollider>();
        if (trigger != null) trigger.radius = Mathf.Max(trigger.radius, 1f);
    }

    private void Update()
    {
        if (collected) return;
        if (visual != null) visual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        // CharacterController trigger callbacks can be missed when a track object
        // is activated after the player has already entered its trigger. Keep a
        // bounds-based overlap fallback so contact still reaches GameManager.
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        Collider keyCollider = GetComponent<Collider>();
        CharacterController playerCollider = player != null ? player.GetComponent<CharacterController>() : null;
        if (player != null && playerCollider != null && keyCollider != null && keyCollider.enabled &&
            keyCollider.bounds.Intersects(playerCollider.bounds))
            TryCollect(player, "bounds-overlap", null);

        if (!collected && !passageDiagnosticLogged && player != null && player.transform.position.z > transform.position.z + 3f)
        {
            passageDiagnosticLogged = true;
            Debug.Log("[KEY-DIAG] passed-without-collection keyId=" + keyId +
                      " playerPosition=" + player.transform.position + " keyPosition=" + transform.position +
                      " keyActive=" + gameObject.activeInHierarchy + " keyColliderEnabled=" +
                      (keyCollider != null && keyCollider.enabled) + " boundsOverlap=" +
                      (playerCollider != null && keyCollider != null && keyCollider.bounds.Intersects(playerCollider.bounds)) +
                      " gameManagerPresent=" + (GameManager.Instance != null), this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        Debug.Log("[KEY-DIAG] trigger-entry keyId=" + keyId + " otherCollider=" + other.name +
                  " playerDetected=" + (player != null) + " playerPosition=" +
                  (player != null ? player.transform.position.ToString() : "n/a") +
                  " keyPosition=" + transform.position, this);
        TryCollect(player, "trigger-enter", other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!collected) TryCollect(other.GetComponentInParent<PlayerMovement>(), "trigger-stay", other);
    }

    private void TryCollect(PlayerMovement player, string source, Collider other)
    {
        if (collected)
        {
            return;
        }
        if (player == null)
        {
            return;
        }
        if (GameManager.Instance == null)
        {
            Debug.Log("[KEY-DIAG] collection-rejected keyId=" + keyId + " reason=GameManager-missing", this);
            return;
        }
        bool accepted = GameManager.Instance.CollectTreasureKey(keyId);
        if (accepted) AudioManager.Play("sfx_key");
        Debug.Log("[KEY-DIAG] collection-result keyId=" + keyId + " source=" + source +
                  " accepted=" + accepted + " playerPosition=" + player.transform.position +
                  " keyPosition=" + transform.position + " otherCollider=" +
                  (other != null ? other.name : "bounds-overlap"), this);
        if (!accepted) return;
        collected = true;
        Collider trigger = GetComponent<Collider>();
        if (trigger != null) trigger.enabled = false;
        if (visual != null) visual.gameObject.SetActive(false);
        if (collectSparkles != null)
        {
            collectSparkles.transform.position = transform.position;
            collectSparkles.transform.SetParent(null, true);
            collectSparkles.Play(true);
            Destroy(collectSparkles.gameObject, 2f);
        }
        Destroy(gameObject);
    }
}
