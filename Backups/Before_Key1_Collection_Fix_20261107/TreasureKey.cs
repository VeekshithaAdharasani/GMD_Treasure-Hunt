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
            ParticleSystem.MainModule main = collectSparkles.main;
            main.duration = 0.65f;
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
        collectSparkles.transform.localScale = Vector3.one * 1.35f;
        collectSparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SphereCollider trigger = GetComponent<SphereCollider>();
        if (trigger != null) trigger.radius = Mathf.Max(trigger.radius, 1f);
    }

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
            collectSparkles.transform.position = transform.position;
            collectSparkles.transform.SetParent(null, true);
            collectSparkles.Play(true);
            Destroy(collectSparkles.gameObject, 2f);
        }
        Destroy(gameObject);
    }
}
