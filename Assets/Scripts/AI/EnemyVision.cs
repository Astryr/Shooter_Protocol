using StarterAssets;
using UnityEngine;

/// <summary>
/// Line of Sight compartido. El raycast golpea el primer collider;
/// solo cuenta como "visible" si es el jugador (tag o componentes).
/// Obstáculos y paredes bloquean la visión.
/// </summary>
public static class EnemyVision
{
    const string PLAYER_TAG = "Player";

    public static bool CanSeePlayer(
        Vector3 observerPosition,
        Vector3 playerPosition,
        float visionRange,
        LayerMask visionLayers)
    {
        Vector3 origin = observerPosition + Vector3.up;
        Vector3 target = playerPosition + Vector3.up;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance > visionRange) return false;
        if (distance < 0.01f) return true;

        bool hitSomething;
        RaycastHit hit;

        if (visionLayers.value == 0)
        {
            hitSomething = Physics.Raycast(
                origin,
                direction.normalized,
                out hit,
                distance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
        }
        else
        {
            hitSomething = Physics.Raycast(
                origin,
                direction.normalized,
                out hit,
                distance,
                visionLayers,
                QueryTriggerInteraction.Ignore);
        }

        if (!hitSomething) return false;

        if (hit.collider.CompareTag(PLAYER_TAG)) return true;

        return hit.collider.GetComponentInParent<PlayerHealth>() != null
            || hit.collider.GetComponentInParent<FirstPersonController>() != null;
    }
}
