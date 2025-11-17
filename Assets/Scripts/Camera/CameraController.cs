using UnityEngine;
using Fusion;

public class CameraController : NetworkBehaviour
{
    [Header("FPS 카메라 설정")]
    public Transform cameraPivot;      // 플레이어 머리 위치
    public float mouseSensitivity = 2f;
    public float minPitch = -70f;
    public float maxPitch = 80f;

    private float yaw;
    private float pitch;
    private Camera cam;
    private PlayerMovement playerMovement;

    [Header("조준 (ADS) 설정")]
    public float normalFOV = 60f;
    public float aimFOV = 40f;
    public float aimLerpSpeed = 12f;

    public Vector3 normalCamOffset = Vector3.zero;
    public Vector3 aimCamOffset = new Vector3(0f, -0.05f, 0.07f);
    public float aimMoveLerpSpeed = 10f;

    public bool isAiming = false;

    public override void Spawned()
    {
        Debug.Log("[CameraController] Spawned() 호출됨");

        cam = GetComponent<Camera>();   // null 방지용 미리 선언
        var audio = GetComponent<AudioListener>();


        if (Object.HasInputAuthority) {
            // 로컬 클라이언트 = 카메라만 활성화
            cam.enabled = true;
            if (audio) audio.enabled = true;
        } else {
            // 다른 플레이어 = 카메라 렌더만 끔, 오브젝트는 비활성화 시키지 않음
            cam.enabled = false;
            if (audio) audio.enabled = false;
        }

        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    void Start()
    {
        if (cam == null)
        cam = GetComponent<Camera>();

        if (cam != null) {
            cam.fieldOfView = normalFOV;
            Debug.Log("[CameraController] cam 초기화 완료");
        }
        

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraPivot == null)
            Debug.LogError("[CameraController] cameraPivot이 연결되지 않았습니다!");
    }

    void Update()
    {
        if (!Object.HasInputAuthority) return;

        isAiming = Input.GetMouseButton(1);

        HandleMouseLook();
        HandleCameraPosition();
        HandleAiming();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleMouseLook()
    {
        // 마우스 입력
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;

        // 상하 회전 제한
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // 카메라 회전 적용 (Pitch만)
        transform.localRotation = Quaternion.Euler(pitch, 0, 0);

        // 플레이어 Yaw 회전
        if (cameraPivot != null) {
            cameraPivot.localRotation = Quaternion.Euler(0, yaw, 0);
        }
    }

    private void HandleCameraPosition()
    {
        if (cameraPivot == null) return;

        // 조준 여부에 따라 Pivot에서 위치 오프셋 추가
        Vector3 targetOffset = isAiming ? aimCamOffset : normalCamOffset;

        Vector3 targetPos = cameraPivot.position + 
                            cameraPivot.TransformVector(targetOffset);

        // FPS 카메라는 항상 머리 위치에서 갱신됨
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            aimMoveLerpSpeed * Time.deltaTime
        );
    }

    private void HandleAiming()
    {
        if (cam == null) return;

        float targetFOV = isAiming ? aimFOV : normalFOV;

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            aimLerpSpeed * Time.deltaTime
        );
    }
}
