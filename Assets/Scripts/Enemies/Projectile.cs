using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float speed = 30f;
    [SerializeField] GameObject projectileHitVFX;

    Rigidbody rb;
    int damage;

    static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

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
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend == null) return;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        rend.GetPropertyBlock(block);
        block.SetColor(EmissionColorID, color);
        block.SetColor(BaseColorID, color);
        rend.SetPropertyBlock(block);
    }

    void OnTriggerEnter(Collider other) 
    {
        if (other.GetComponentInParent<EnemyHealth>() != null) return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        playerHealth?.TakeDamage(damage);

        if (projectileHitVFX != null)
        {
            Instantiate(projectileHitVFX, transform.position, Quaternion.identity);
        }
        Destroy(this.gameObject);
    }
}
