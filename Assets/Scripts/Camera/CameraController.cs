using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("기본 설정")]
    public Transform playerTransform;
    public Vector3 offset = new Vector3(0f, 3f, -6f);
    public float rotationSpeed = 3f;
    
    [Header("조준 모드")]
    public bool isAiming = false;
    public Vector3 aimOffset = new Vector3(0f, 2f, -3f);
    public float aimTransitionSpeed = 5f;
    public float aimSensitivity = 1.5f;

    [Header("사망 시점")]
    public bool isDeathView = false;
    private Quaternion deathViewRotation;

    [Header("플레이어 동기화")]
    public bool syncPlayerYaw = true;
    public float playerYawSpeed = 12f;   // 회전 속도(스무딩)

    private float mouseX, mouseY;
    private Camera cam;

    void Start()
    {
        if (playerTransform == null)
            playerTransform = GameObject.Find("Player").transform;

        cam = GetComponent<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseRotation();
        HandleAimInput();
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        if (isDeathView)
        {
            // 사망 시 상공 시점 고정
            transform.position = Vector3.Lerp(
                transform.position,
                playerTransform.position + new Vector3(0, 15f, -10f),
                Time.deltaTime
            );
            transform.rotation = Quaternion.Slerp(transform.rotation, deathViewRotation, Time.deltaTime);
            return;
        }

        // 마우스 입력 받아 카메라 회전
        Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0f);
        transform.rotation = rotation;

        // 카메라 Yaw(mouseX)와 플레이어 Yaw를 동기화
        if (syncPlayerYaw && playerTransform != null && !isDeathView)
        {
            // 플레이어는 수평(Yaw)만 회전하도록 pitch(상하)는 제외
            Quaternion playerTargetRot = Quaternion.Euler(0f, mouseX, 0f);

            // 스무스 회전
            playerTransform.rotation = Quaternion.Slerp(
                playerTransform.rotation,
                playerTargetRot,
                playerYawSpeed * Time.deltaTime
            );
        }

        // 카메라 위치는 플레이어 기준 고정 (오프셋만 적용)
        Vector3 desiredOffset = isAiming ? aimOffset : offset;
        // Vector3 targetPosition = playerTransform.position + rotation * desiredOffset;
        Vector3 targetPosition = playerTransform.position + desiredOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * aimTransitionSpeed);
    }



    private void HandleMouseRotation()
    {
        if (isDeathView) return;

        float sens = isAiming ? aimSensitivity : rotationSpeed;

        mouseX += Input.GetAxis("Mouse X") * sens;
        mouseY -= Input.GetAxis("Mouse Y") * sens;
        mouseY = Mathf.Clamp(mouseY, -35f, 60f);
    }

    private void HandleAimInput()
    {
        isAiming = Input.GetMouseButton(1);

        // 조준 시 FOV 변경
        float targetFov = isAiming ? 40f : 60f;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * 5f);
    }

    // -------- 이벤트 핸들러 --------
    public void OnPlayerDeath()
    {
        isDeathView = true;
        deathViewRotation = Quaternion.Euler(45f, 0f, 0f);
    }

    public void OnPlayerRespawn()
    {
        isDeathView = false;
    }
}
