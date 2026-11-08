using UnityEngine;

public class Collectible : MonoBehaviour
{
    public enum TreasureType { BlueCrystal, GoldCrystal, Coin }
    public enum PowerUpType { None, CoinMagnet, Shield, SpeedBoost, ScoreMultiplier, TreasureBonus }
    public TreasureType treasureType = TreasureType.BlueCrystal;
    public PowerUpType powerUpType = PowerUpType.None;
    public bool purpleCrystalVariant;
    public int points = 10;
    public float spinSpeed = 45f;

    private bool collected;
    private MaterialPropertyBlock powerUpBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (purpleCrystalVariant)
        {
            Renderer crystal = GetComponentInChildren<Renderer>(true);
            if (crystal != null && crystal.sharedMaterial != null)
            {
                MaterialPropertyBlock crystalBlock = new MaterialPropertyBlock();
                crystal.GetPropertyBlock(crystalBlock);
                Color purple = new Color(0.68f, 0.28f, 0.95f);
                if (crystal.sharedMaterial.HasProperty(BaseColorId)) crystalBlock.SetColor(BaseColorId, purple);
                if (crystal.sharedMaterial.HasProperty(ColorId)) crystalBlock.SetColor(ColorId, purple);
                crystal.SetPropertyBlock(crystalBlock);
            }
        }
        if (powerUpType == PowerUpType.None) return;
        Transform distinctiveVisual = transform.Find("POWERUP_VISUAL_ROOT");
        if (distinctiveVisual != null)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                bool isPickupShape = renderers[i].transform == distinctiveVisual || renderers[i].transform.IsChildOf(distinctiveVisual);
                renderers[i].enabled = isPickupShape;
            }
            return;
        }
        Renderer target = GetComponentInChildren<Renderer>(true);
        if (target == null || target.sharedMaterial == null) return;
        powerUpBlock = new MaterialPropertyBlock();
        target.GetPropertyBlock(powerUpBlock);
        Color tint = GetPowerUpTint(powerUpType);
        if (target.sharedMaterial.HasProperty(BaseColorId)) powerUpBlock.SetColor(BaseColorId, tint);
        if (target.sharedMaterial.HasProperty(ColorId)) powerUpBlock.SetColor(ColorId, tint);
        target.SetPropertyBlock(powerUpBlock);
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        if (powerUpType != PowerUpType.None || treasureType != TreasureType.Coin ||
            GameManager.Instance == null || !GameManager.Instance.CoinMagnetActive) return;
        Transform player = GameManager.Instance.PlayerTransform;
        if (player == null || Vector3.SqrMagnitude(player.position - transform.position) > GameManager.Instance.CoinMagnetRadius * GameManager.Instance.CoinMagnetRadius) return;
        Vector3 target = player.position + Vector3.up * 0.65f;
        transform.position = Vector3.MoveTowards(transform.position, target, GameManager.Instance.CoinMagnetSpeed * Time.deltaTime);
    }

    private static Color GetPowerUpTint(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.CoinMagnet: return new Color(1f, 0.68f, 0.12f);
            case PowerUpType.Shield: return new Color(0.1f, 0.9f, 1f);
            case PowerUpType.SpeedBoost: return new Color(1f, 0.32f, 0.06f);
            case PowerUpType.ScoreMultiplier: return new Color(0.75f, 0.28f, 1f);
            case PowerUpType.TreasureBonus: return new Color(1f, 0.86f, 0.12f);
            default: return Color.white;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.GetComponentInParent<PlayerMovement>()) return;
        collected = true;
        if (powerUpType != PowerUpType.None) AudioManager.Play("sfx_powerup");
        else if (treasureType == TreasureType.Coin) AudioManager.Play("sfx_coin");
        else AudioManager.Play("sfx_crystal");
        int awardedPoints = treasureType == TreasureType.GoldCrystal ? Mathf.Max(points, 50) : points;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCollectible(treasureType, awardedPoints);
            if (powerUpType != PowerUpType.None) GameManager.Instance.ActivatePowerUp(powerUpType);
        }
        Destroy(gameObject);
    }
}
