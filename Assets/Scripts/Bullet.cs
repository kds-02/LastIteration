using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public float lifetime = 3f;

    [Header("Combat")]
    public float damage = 34f;
    public int shooterId = -1;  // Gun에서 세팅

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        Destroy(gameObject, lifetime);
        if (rb != null) rb.velocity = transform.forward * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        // 플레이어 여부 탐색(자식 콜라이더 고려)
        var state = collision.collider.GetComponentInParent<PlayerState>();
        if (state != null && !state.IsDead())
        {
            state.TakeDamage(damage, shooterId);
        }

        Destroy(gameObject);
    }
}
