using Fusion;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [Networked] public float Hp { get; set; } = 100f;
    [Networked] public float MaxHp { get; set; } = 100f;
    [Networked] public float Kill { get; set; } = 0f;
    [Networked] public float Death { get; set; } = 0f;
    [Networked] public NetworkBool IsDead { get; set; } = false;
    [Networked] private TickTimer RespawnTimer { get; set; }

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 3f;

    [Header("Respawn Area")]
    [SerializeField] private float respawnEdge = 14f;

    private Collider[] colliders;
    private Renderer[] renderers;
    private Rigidbody rb;
    private Animator animator;
    private CharacterController characterController;
    private Spawner spawner;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Hp = MaxHp;
            IsDead = false;
        }

        colliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        // ★ 서버에서만 부활 위치 스포너 참조
        if (Object.HasStateAuthority)
            spawner = Spawner.Instance ?? FindObjectOfType<Spawner>();
    }

    // ------- 입력 테스트(L, K) ------- //
    private void Update()
    {
        // 입력 권한 있는 로컬 클라이언트만
        if (!Object.HasInputAuthority) return;
        if (IsDead) return;

        // L: 강제 즉사
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("[PlayerState] L key pressed → instant death test");
            RPC_TakeDamage(9999f, -1);
        }

        // K: 10 데미지
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("[PlayerState] K key pressed → 10 damage test");
            RPC_TakeDamage(10f, -1);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority && IsDead && RespawnTimer.ExpiredOrNotRunning(Runner))
        {
            Respawn();
        }
    }

    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_TakeDamage(float damage, int attackerId)
    {
        if (IsDead) return;

        // 서버에서 실제 HP 감소
        float before = Hp;
        Hp -= Mathf.Max(0f, damage);
        Debug.Log($"[PlayerState] RPC_TakeDamage on StateAuthority: {before} -> {Hp}");

        if (Hp <= 0f)
        {
            Die(attackerId);
        }
    }

    private void Die(int attackerId)
    {
        if (!Object.HasStateAuthority || IsDead) return;

        Debug.Log("[PlayerState] Die() called");

        IsDead = true;
        Death += 1f;

        RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelay);

        RPC_PlayDeathAnimation();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayDeathAnimation()
    {
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

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        if (colliders != null)
        {
            foreach (var c in colliders) c.enabled = false;
        }
    }

    private void Respawn()
    {
        if (!Object.HasStateAuthority) return;

        // ▼ 1) 스폰 포인트에서 위치/회전 가져오기
        Vector3 respawnPos;
        Quaternion respawnRot;

        if (spawner != null)
        {
            Transform spawn = spawner.GetRandomSpawnPoint();
            respawnPos = spawn.position;
            respawnRot = spawn.rotation;
        }
        else
        {
            // 혹시 Spawner 못 찾았을 때 대비용 (필요 없으면 빼도 됨)
            respawnPos = transform.position;
            respawnRot = transform.rotation;
        }

        // ▼ 2) 체력/상태 초기화
        Hp = MaxHp;
        IsDead = false;

        var pm = GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.ResetMovementState();   // 기존 속도 초기화
            pm.OnRespawnFreeze(1);     // ★ 이 틱 동안 이동 막기 (필요하면 2로 늘려도 됨)
        }

        if (characterController != null)
            characterController.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        var nt = GetComponent<NetworkTransform>();
        if (nt != null)
            nt.Teleport(respawnPos, respawnRot);
        else
            transform.SetPositionAndRotation(respawnPos, respawnRot);

        if (characterController != null)
            characterController.enabled = true;

        if (rb != null)
            rb.isKinematic = false;

        RPC_PlayRespawnAnimation();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayRespawnAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsDead", false);
            animator.Rebind();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (characterController != null)
        {
            characterController.enabled = true;
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

    public float GetRespawnRemaining()
    {
        if (!IsDead || RespawnTimer.ExpiredOrNotRunning(Runner))
            return 0f;

        return RespawnTimer.RemainingTime(Runner) ?? 0f;
    }

    public float GetKill() => Kill;
    public float GetDeath() => Death;
    public float GetHp() => Hp;
    public float GetMaxHp() => MaxHp;

    private Vector3 GetRandomEdgePosition()
    {
        float edge = respawnEdge;
        float y = transform.position.y;

        float t = Random.Range(-edge, edge);
        int side = Random.Range(0, 4);

        float x = 0f;
        float z = 0f;

        switch (side)
        {
            case 0: x = edge; z = t; break;
            case 1: x = -edge; z = t; break;
            case 2: z = edge; x = t; break;
            case 3: z = -edge; x = t; break;
        }

        return new Vector3(x, y, z);
    }

    private Quaternion GetLookAtCenterRotation(Vector3 position)
    {
        Vector3 dir = Vector3.zero - position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.forward;

        return Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}
