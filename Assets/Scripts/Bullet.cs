using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 3f;
    [Header("Combat")]
    public float damage = 34f;
    public int shooterId = -1;

    private Rigidbody rb;
    private bool velocitySet = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }

    void Start()
    {
        // OnEnable 대신 Start에서 velocity 설정
        if (rb != null && !velocitySet)
        {
            rb.velocity = transform.forward * speed;
            velocitySet = true;
            Debug.Log($"[Bullet] Velocity set: {rb.velocity}, forward: {transform.forward}");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void HandleHit(Collider target)
    {
        // ✅ 총알끼리 충돌 무시 (컴포넌트로만 체크)
        if (target.GetComponent<Bullet>() != null)
        {
            Debug.Log("[Bullet] Ignored bullet-to-bullet collision");
            return;
        }

        var hitbox = target.GetComponent<PlayerHitbox>() ?? target.GetComponentInParent<PlayerHitbox>();
        if (hitbox != null)
        {
            hitbox.ApplyDamage(damage, shooterId);
            Destroy(gameObject);
            return;
        }

        var state = target.GetComponentInParent<PlayerState>();
        if (state != null && !state.IsDead)
        {
            state.RPC_TakeDamage(damage, shooterId);
        }
        Destroy(gameObject);
    }
}