using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float airControl = 5f;

    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private bool isGrounded;
    private bool isCrouching = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        standingHeight = controller.height;
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Ctrl을 누르고 있으면 앉기
        bool wantsToCrouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (wantsToCrouch && !isCrouching)
        {
            // 앉기
            isCrouching = true;
            controller.height = crouchingHeight;
            controller.center = new Vector3(0, crouchingHeight / 2, 0);
        }
        else if (!wantsToCrouch && isCrouching)
        {
            // 일어서기
            isCrouching = false;
            controller.height = standingHeight;
            controller.center = new Vector3(0, standingHeight / 2, 0);
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        if (Input.GetKey(KeyCode.S)) vertical = -1f;
        if (Input.GetKey(KeyCode.A)) horizontal = -1f;
        if (Input.GetKey(KeyCode.D)) horizontal = 1f;

        // 수정: 카메라가 바라보는 방향 기준으로 이동
        Transform cam = Camera.main.transform;

        // 카메라의 앞/오른쪽 벡터 (y값 제거 → 평지 이동 유지)
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 inputDirection = camRight * horizontal + camForward * vertical;

        // 플레이어를 이동 방향으로 회전시키기
        if (inputDirection.magnitude > 0.1f)
        {
            // 목표 회전
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);

            // 스무스 회전
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime        // 회전 속도 (필요하면 조절 가능)
            );
        }


        // 속도 결정: 앉기 > 걷기(기본) > 뛰기(Shift)
        float targetSpeed;
        if (isCrouching)
        {
            targetSpeed = crouchSpeed;
        }
        else
        {
            targetSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        }

        // 목표 속도 벡터
        Vector3 targetVelocity = inputDirection * targetSpeed;

        // 땅에 있을 때와 공중일 때 다르게 처리
        if (isGrounded)
        {
            // 땅에 있을 때: 가속/감속을 부드럽게
            float smoothFactor = (inputDirection.magnitude > 0) ? acceleration : deceleration;
            currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, smoothFactor * Time.deltaTime);
        }
        else
        {
            // 공중에 있을 때: 약간의 제어만 가능, 관성 유지
            if (inputDirection.magnitude > 0)
            {
                currentMoveVelocity = Vector3.Lerp(currentMoveVelocity, targetVelocity, airControl * Time.deltaTime);
            }
            // 키를 떼면 속도 유지 (감속 안 함)
        }

        controller.Move(currentMoveVelocity * Time.deltaTime);

        // 앉은 상태에서는 점프 불가
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Animator 파라미터 업데이트
        if (animator != null)
        {
            // 로컬 좌표계로 변환 (캐릭터 기준 방향)
            Vector3 localVelocity = transform.InverseTransformDirection(currentMoveVelocity);

            animator.SetFloat("Horizontal", localVelocity.x);
            animator.SetFloat("Vertical", localVelocity.z);

            // Speed: 0 = Walk, 1 = Run
            float speed = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;
            animator.SetFloat("Speed", speed);

            animator.SetBool("IsCrouching", isCrouching);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetBool("IsJumping", !isGrounded);
        }
    }
}
