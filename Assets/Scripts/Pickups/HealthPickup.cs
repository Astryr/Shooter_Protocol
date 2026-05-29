using UnityEngine;

/// <summary>
/// Cajas de suministro: restauran vida del jugador (antes daban munición).
/// </summary>
public class HealthPickup : Pickup
{
    [SerializeField] int healthAmount = 2;

    protected override bool TryPickupHealth(PlayerHealth playerHealth)
    {
        if (playerHealth == null) return false;

        playerHealth.Heal(healthAmount);
        return true;
    }

    protected override void OnPickup(ActiveWeapon activeWeapon) { }
}
