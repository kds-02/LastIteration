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
    // private NetworkCharacterController netCC;
    private CameraController localCam;

    // 네트워크로 스폰될 때 호출됨
    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();
        // netCC = GetComponent<NetworkCharacterController>();
        animator = GetComponent<Animator>();

        standingHeight = controller.height;

        // 로컬 플레이어일 때만 카메라를 연결
        if (Object.HasInputAuthority)
            StartCoroutine(SetupCamera());
    }

    // 카메라 연결 지연 처리 (Fusion 초기화 완료 기다림)
    private IEnumerator SetupCamera()
    {
        yield return new WaitForSeconds(0.2f);

        // 로컬 플레이어만 카메라 연결
        if (!Object.HasInputAuthority)
            yield break;
        
        Transform pivot = transform.Find("CameraPivot");

        // CameraController가 생성/활성화될 때까지 계속 대기
        while (localCam == null)
        {
            var camCtrl = GetComponentInChildren<CameraController>(true);
            if (camCtrl != null)
            {
                camCtrl.cameraPivot = pivot;
                localCam = camCtrl;
                Debug.Log("[Fusion] FPS 카메라 Pivot 연결 완료");
                yield break;
            }

        yield return null; // 다음 프레임에서 다시 탐색
    }

        // // Camera.main 제거 !!
        // var camCtrl = GetComponentInChildren<CameraController>();
        // if (camCtrl != null)
        // {
        //     camCtrl.cameraPivot = transform.Find("CameraPivot");
        //     localCam = camCtrl;   // ⭐ 로컬 카메라 저장
        //     Debug.Log("[Fusion] FPS 카메라 Pivot 연결 완료");
        // }
        // else
        // {
        //     Debug.LogError("[Fusion] CameraController를 찾지 못했습니다!");
        // }
    }

    // Fusion의 FixedUpdateNetwork() — 네트워크 프레임마다 실행됨
    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData data))
            return;

        HandleMovement(data);
        HandleAnimation();
    }

    //   이동 처리
    private void HandleMovement(NetworkInputData data)
    {
        if (localCam == null)
        return; // 카메라 초기화 전에는 이동 처리 안 함

        // --- 땅 체크 ---
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;  // 땅을 붙잡는 용도

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

        // --- 카메라 기준 이동 방향 계산 ---
        // Transform cam = Camera.main.transform;
        Transform cam = localCam.transform;

        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0; camForward.Normalize();
        camRight.y = 0; camRight.Normalize();

        Vector3 inputDirection = camRight * data.moveInput.x + camForward * data.moveInput.y;

        // --- 캐릭터 회전 (Yaw)(이동 방향 바라보기) ---
        float yaw = localCam.GetYaw();
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        // if (inputDirection.magnitude > 0.1f)
        // {
        //     Quaternion targetRot = Quaternion.LookRotation(inputDirection);
        //     transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Runner.DeltaTime);
        // }

        // --- 이동 속도 결정 (걷기 / 뛰기 / 앉기) ---
        float targetSpeed =
            isCrouching ? crouchSpeed :
            data.runHeld ? runSpeed :
            walkSpeed;

        Vector3 targetVelocity = inputDirection * targetSpeed;

        // --- 가속/감속 적용 ---
        if (isGrounded)
        {
            float factor = inputDirection.magnitude > 0 ? acceleration : deceleration;
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, factor * Runner.DeltaTime);
        }
        else
        {
            // 공중에서 이동 입력이 있을 때만 제어
            if (inputDirection.magnitude > 0)
            {
                currentMoveVelocity =
                    Vector3.Lerp(currentMoveVelocity, targetVelocity, airControl * Runner.DeltaTime);
            }
        }

        // 실제 이동 적용
        controller.Move(currentMoveVelocity * Runner.DeltaTime);

        // --- 점프 처리 ---
        if (data.jumpPressed && isGrounded && !isCrouching)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // --- 중력 적용 ---
        velocity.y += gravity * Runner.DeltaTime;
        controller.Move(velocity * Runner.DeltaTime);
    }

    //   애니메이션 처리
    private void HandleAnimation()
    {
        if (animator == null) return;

        // 현재 이동 벡터를 로컬 공간 기준으로 변환
        Vector3 localVel = transform.InverseTransformDirection(currentMoveVelocity);

        animator.SetFloat("Horizontal", localVel.x);
        animator.SetFloat("Vertical", localVel.z);

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCrouching", isCrouching);
        animator.SetBool("IsJumping", !isGrounded);

        // Speed(걷기/뛰기 전환값) — 로컬 플레이어에게만 적용
        float speedParam = Object.HasInputAuthority && Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;
        animator.SetFloat("Speed", speedParam);
    }
}
