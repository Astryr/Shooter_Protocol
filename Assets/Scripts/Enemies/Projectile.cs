using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 30f;
    [SerializeField] GameObject projectileHitVFX;

    Rigidbody rb;

    int damage;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        rb.linearVelocity = transform.forward * speed;
    }

    public void Init(int damage) 
    {
        this.damage = damage;
    }

    public void SetColor(Color color)
    {
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color);
            mat.color = color;
        }
    }

    void OnTriggerEnter(Collider other) 
    {
        // Evitar que el proyectil se destruya al colisionar con el propio enemigo o sus partes
        if (other.GetComponentInParent<EnemyHealth>() != null) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        playerHealth?.TakeDamage(damage);

        if (projectileHitVFX != null)
        {
            Instantiate(projectileHitVFX, transform.position, Quaternion.identity);
        }
        Destroy(this.gameObject);
    }
}
