using UnityEngine;

public abstract class Pickup : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 100f;

    const string PLAYER_STRING = "Player";

    void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PLAYER_STRING)) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (TryPickupHealth(playerHealth))
        {
            Destroy(gameObject);
            return;
        }

        ActiveWeapon activeWeapon = other.GetComponentInChildren<ActiveWeapon>();
        if (activeWeapon == null)
        {
            Debug.LogWarning($"[Pickup] No se encontró ActiveWeapon en el jugador. El pickup no se aplicará.", this);
            return;
        }

        OnPickup(activeWeapon);
        Destroy(gameObject);
    }

    protected virtual bool TryPickupHealth(PlayerHealth playerHealth) => false;

    protected abstract void OnPickup(ActiveWeapon activeWeapon);
}
