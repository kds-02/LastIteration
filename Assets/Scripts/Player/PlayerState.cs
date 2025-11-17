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

    private Collider[] colliders;
    private Renderer[] renderers;
    private Rigidbody rb;
    private Animator animator;
    private CharacterController characterController;

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

        Hp -= Mathf.Max(0f, damage);

        if (Hp <= 0f)
        {
            Die(attackerId);
        }
    }

    private void Die(int attackerId)
    {
        if (!Object.HasStateAuthority || IsDead) return;

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

        Vector3 respawnPos = transform.position;
        Quaternion respawnRot = transform.rotation;

        Hp = MaxHp;
        IsDead = false;

        RPC_PlayRespawnAnimation(respawnPos, respawnRot);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayRespawnAnimation(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);

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
}
