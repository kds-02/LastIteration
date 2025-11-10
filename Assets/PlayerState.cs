using System.Collections;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField] private float hp = 100f;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float kill = 0f;   // 추후 서버에서 관리할 예정
    [SerializeField] private float death = 0f;
    [SerializeField] private bool isDead = false;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 3f;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float respawnEndTime = -1f;   // 사망 시점 + respawnDelay

    private Collider[] colliders;
    private Renderer[] renderers;
    private Rigidbody rb;

    void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        colliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
        rb = GetComponent<Rigidbody>();
    }

    // 외부에서 상태 확인
    public bool IsDead() => isDead;

    public void TakeDamage(float damage, int attackerId)
    {
        if (isDead) return;

        hp -= Mathf.Max(0f, damage);
        if (hp <= 0f)
        {
            Die(attackerId);
        }
    }

    private void Die(int attackerId)
    {
        if (isDead) return;

        isDead = true;
        death += 1f;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        SetAliveVisual(false);

        //리스폰 종료 시각 기록
        respawnEndTime = Time.time + respawnDelay;

        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    private void Respawn()
    {
        // 위치/회전 초기화
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        // 체력 복구
        hp = maxHp;
        isDead = false;

        // 물리/충돌/렌더 복구
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        SetAliveVisual(true);

    }

    private void SetAliveVisual(bool alive)
    {
        if (colliders != null) foreach (var c in colliders) c.enabled = alive;
        if (renderers != null) foreach (var r in renderers) r.enabled = alive;
    }

    public float GetKill() => kill;
    public float GetDeath() => death;

    // 사망 중일 때만 남은 초 반환, 생존 중이면 0
    public float GetRespawnRemaining()
    {
        if (!isDead) return 0f;
        return Mathf.Max(0f, respawnEndTime - Time.time);
    }
}
