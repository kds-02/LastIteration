using Fusion;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("이동 속도 설정")]
    [SerializeField] private float walkSpeed = 3f;      // 기본 걷기 속도
    [SerializeField] private float runSpeed = 6f;       // 뛰기 속도
    [SerializeField] private float crouchSpeed = 2f;    // 앉기 속도
    [SerializeField] private float jumpHeight = 1.2f;   // 점프 높이
    [SerializeField] private float gravity = -9.81f;    // 중력 값

    [Header("가속 / 감속 설정")]
    [SerializeField] private float acceleration = 10f;  // 가속도
    [SerializeField] private float deceleration = 10f;  // 감속도
    [SerializeField] private float airControl = 5f;     // 공중에서의 제어력

    [Header("앉기 설정")]
    [SerializeField] private float standingHeight = 2f;     // 서 있을 때 높이
    [SerializeField] private float crouchingHeight = 1f;    // 앉았을 때 높이

    private CharacterController controller;
    private Animator animator;

    private Vector3 velocity;             // 수직 속도(중력/점프)
    private Vector3 currentMoveVelocity;  // 실제 이동 벡터 (가속/감속 반영)

    private bool isGrounded;              // 땅에 닿았는지
    private bool isCrouching;             // 앉은 상태인지

    private CameraController localCam;

    [Networked] public float NetworkYaw { get; set; } // 네트워크 회전값

    // 리스폰 직후 이동 잠깐 막기용
    private int freezeTicksAfterRespawn = 0;

    // 네트워크로 스폰될 때 호출됨
    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        standingHeight = controller.height;

        // 로컬 플레이어일 때만 카메라를 연결
        if (Object.HasInputAuthority)
            StartCoroutine(SetupCamera());
    }

    // 카메라 연결
    private IEnumerator SetupCamera()
    {
        yield return new WaitForSeconds(0.2f); // 지연 처리 (Fusion 초기화 완료 기다림)

        // 로컬 플레이어만 카메라 연결
        if (!Object.HasInputAuthority)
            yield break;

        Transform pivot = transform.Find("CameraPivot");

        // 최대 10초간 대기 (무한 루프 방지)
        float timeout = 10f;
        float elapsed = 0f;

        // CameraController가 생성/활성화될 때까지 대기
        while (localCam == null && elapsed < timeout)
        {
            var camCtrl = GetComponentInChildren<CameraController>(true);
            if (camCtrl != null)
            {
                camCtrl.cameraPivot = pivot;
                localCam = camCtrl;
                Debug.Log("[Fusion] FPS 카메라 Pivot 연결 완료");
                yield break;
            }

            yield return null;
            elapsed += Time.deltaTime;
        }

        if (localCam == null)
        {
            Debug.LogError("[Fusion] CameraController 연결 실패 (Timeout)");
        }
    }

    public override void FixedUpdateNetwork() // 네트워크 프레임마다 실행됨
    {
        // ★ 먼저 죽었으면 이동/애니메이션 전부 스킵
        var state = GetComponent<PlayerState>();
        if (state != null && state.IsDead)
            return;

        // ★ 리스폰 직후 N틱 동안 이동 완전 차단
        if (freezeTicksAfterRespawn > 0)
        {
            freezeTicksAfterRespawn--;

            // 안전하게 속도도 0으로
            currentMoveVelocity = Vector3.zero;
            velocity = Vector3.zero;

            // 애니메이션도 멈춘 상태로 두고 싶으면 여기서 바로 return
            return;
        }

        if (!GetInput(out NetworkInputData data))
            return;

        // 서버만 이동 계산
        if (Object.HasStateAuthority)
        {
            HandleMovementServer(data);
        }

        // 애니메이션은 모두 공통으로
        HandleAnimation(data);
    }

    // 서버 권위 이동 함수
    private void HandleMovementServer(NetworkInputData data)
    {
        // --- 서버에서 Yaw 업데이트 (마우스X입력값 사용) ---
        NetworkYaw += data.mouseDeltaX * 2f; // 회전 감도
        transform.rotation = Quaternion.Euler(0, NetworkYaw, 0);

        // --- 땅 체크 ---
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f; // 땅을 붙잡는 용도

        // --- 앉기 처리 ---
        if (data.crouchHeld && !isCrouching)
        {
            isCrouching = true;
            controller.height = crouchingHeight;
            controller.center = new Vector3(0, crouchingHeight / 2, 0);
        }
        else if (!data.crouchHeld && isCrouching)
        {
            isCrouching = false;
            controller.height = standingHeight;
            controller.center = new Vector3(0, standingHeight / 2, 0);
        }

        // --- 이동 방향 (서버 기준 yaw) ---
        Vector3 forward = transform.forward;
        Vector3 right   = transform.right;

        Vector3 inputDirection =
            right * data.moveInput.x +
            forward * data.moveInput.y;

        // --- 이동 속도 ---
        float targetSpeed =
            isCrouching ? crouchSpeed :
            data.runHeld ? runSpeed :
            walkSpeed;

        Vector3 targetVelocity = inputDirection * targetSpeed;

        // --- 가속/감속 ---
        if (isGrounded)
        {
            float factor = inputDirection.magnitude > 0 ? acceleration : deceleration;
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, factor * Runner.DeltaTime);
        }
        else
        {
            if (inputDirection.magnitude > 0)
                currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, airControl * Runner.DeltaTime);
        }

        // --- 점프 ---
        if (data.jumpPressed && isGrounded && !isCrouching)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // --- 중력 ---
        velocity.y += gravity * Runner.DeltaTime;

        // --- 최종 이동 ---
        controller.Move((currentMoveVelocity + new Vector3(0, velocity.y, 0)) * Runner.DeltaTime);

        // transform.position은 NetworkTransform이 자동으로 싱크됨
    }

    // 애니메이션 처리
    private void HandleAnimation(NetworkInputData data)
    {
        if (animator == null) return;

        // 현재 이동 벡터를 로컬 공간 기준으로 변환
        Vector3 localVel = transform.InverseTransformDirection(currentMoveVelocity);

        animator.SetFloat("Horizontal", localVel.x);
        animator.SetFloat("Vertical", localVel.z);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCrouching", isCrouching);
        animator.SetBool("IsJumping", !isGrounded);

        // Speed(걷기/뛰기 전환값)
        float speedParam = data.runHeld ? 1f : 0f;
        animator.SetFloat("Speed", speedParam);
    }

    // ▼ Respawn 시 PlayerState에서 호출하는 함수들 ▼

    // 속도/중력 상태 초기화
    public void ResetMovementState()
    {
        currentMoveVelocity = Vector3.zero;
        velocity = Vector3.zero;
    }

    // 리스폰 후 n틱 동안 이동 막기
    public void OnRespawnFreeze(int ticks = 1)
    {
        freezeTicksAfterRespawn = Mathf.Max(freezeTicksAfterRespawn, ticks);
    }
}
