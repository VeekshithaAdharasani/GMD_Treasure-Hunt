using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Min(1)]
    public int damage = 1;

    private bool hasDamagedPlayer = false;

    public void ApplyDamageTo(PlayerMovement player)
    {
        if (player == null || hasDamagedPlayer)
            return;

        hasDamagedPlayer = true;
        AudioManager.Play("sfx_hit");
        if (GameManager.Instance != null)
        {
            if (!GameManager.Instance.TryAbsorbDamage())
                GameManager.Instance.TakeDamage(damage);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        ApplyDamageTo(player);
    }
}
