using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [Header("Game State")]
    [SerializeField] private float hp = 100f;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float kill = 0f;
    [SerializeField] private float death = 0f;
    [SerializeField] private bool isDead = false;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 3f;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private double respawnEndTime = -1;   // 네트워크 기준 시간 (Fusion SimulationTime으로 설정)

    private Collider[] colliders;
    private Renderer[] renderers;
    private Rigidbody rb;

    public override void Spawned()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        colliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
        rb = GetComponent<Rigidbody>();
    }

    // �ܺο��� ���� Ȯ��
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

        // 네트워크 시간 기준으로 리스폰 시점 설정
        respawnEndTime = Runner.SimulationTime + respawnDelay;
    }

    public override void FixedUpdateNetwork()
    {
        // 네트워크 시간 기준으로 리스폰 처리
        if (isDead && Runner.SimulationTime >= respawnEndTime)
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        // ��ġ/ȸ�� �ʱ�ȭ
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        
        // ü�� ����
        hp = maxHp;
        isDead = false;

        // ����/�浹/���� ����
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
        if (colliders != null)
            foreach (var c in colliders) c.enabled = alive;
        if (renderers != null)
            foreach (var r in renderers) r.enabled = alive;
    }

    public float GetKill() => kill;
    public float GetDeath() => death;

    // 남은 리스폰 시간 계산 (화면에 출력)
    // ��� ���� ���� ���� �� ��ȯ, ���� ���̸� 0
    public float GetRespawnRemaining()
    {
        if (!isDead) return 0f;
        return Mathf.Max(0f, (float)(respawnEndTime - Runner.SimulationTime));
    }
}