using UnityEngine;

/// <summary>
/// Obsoleto: las armas están en el inventario inicial del jugador.
/// Se desactiva al iniciar para no dejar pickups de armas en el nivel.
/// </summary>
public class WeaponPickup : Pickup
{
    [SerializeField] WeaponSO weaponSO;

    void Start()
    {
        gameObject.SetActive(false);
    }

    protected override void OnPickup(ActiveWeapon activeWeapon) { }
}
