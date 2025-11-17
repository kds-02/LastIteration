using System.Collections;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [Header("Game State")]
    [SerializeField] private float hp = 100f;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float kill = 0f;   // ���� �������� ������ ����
    [SerializeField] private float death = 0f;
    [SerializeField] private bool isDead = false;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 3f;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private float respawnEndTime = -1f;   // ��� ���� + respawnDelay

    private Collider[] colliders;
    private Renderer[] renderers;
    private Rigidbody rb;
    private Animator animator;

    void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        colliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 테스트용: K 키를 누르면 즉사
        if (Input.GetKeyDown(KeyCode.K))
        {
            TakeDamage(999f, -1);
            Debug.Log("테스트: 플레이어 사망!");
        }
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

        if (animator != null)
        {
            animator.SetBool("IsDead", true);
            animator.SetTrigger("Die");
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (colliders != null)
        {
            foreach (var c in colliders) c.enabled = false;
        }

        //������ ���� �ð� ���
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
        // ��ġ/ȸ�� �ʱ�ȭ
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        // ü�� ����
        hp = maxHp;
        isDead = false;

        if (animator != null)
        {
            animator.SetBool("IsDead", false);
            animator.Rebind();
        }

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
        if (colliders != null) foreach (var c in colliders) c.enabled = alive;
        if (renderers != null) foreach (var r in renderers) r.enabled = alive;
    }

    public float GetKill() => kill;
    public float GetDeath() => death;

    // ��� ���� ���� ���� �� ��ȯ, ���� ���̸� 0
    public float GetRespawnRemaining()
    {
        if (!isDead) return 0f;
        return Mathf.Max(0f, respawnEndTime - Time.time);
    }
}
