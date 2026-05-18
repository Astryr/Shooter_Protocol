using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] GameObject robotExplosionVFX;
    [SerializeField] int startingHealth = 3;

    int currentHealth;
    bool isDead = false;

    GameManager gameManager;

    void Awake() 
    {
        currentHealth = startingHealth;
    }

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError($"[EnemyHealth] No se encontró GameManager en la escena. El contador de enemigos no funcionará.", this);
            return;
        }
        gameManager.AdjustEnemiesLeft(1);
    }

    public void TakeDamage(int amount) 
    {
        if (isDead) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            SelfDestruct();
        }
    }

    public void SelfDestruct()
    {
        if (isDead) return;
        isDead = true;

        gameManager?.AdjustEnemiesLeft(-1);
        Instantiate(robotExplosionVFX, transform.position, Quaternion.identity);
        Destroy(this.gameObject);
    }
}
