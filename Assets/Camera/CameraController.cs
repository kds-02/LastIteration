using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("기본 설정")]
    public Transform playerTransform;
    public Vector3 offset = new Vector3(0f, 5f, -8f);
    public float rotationSpeed = 3f;

    [Header("조준 모드 설정")]
    public bool isAiming = false;
    public Vector3 aimOffset = new Vector3(0f, 2f, -3f);
    public float aimTransitionSpeed = 5f;
    public float aimSensitivity = 1.5f;

    [Header("사망 시점 설정")]
    public bool isDeathView = false;
    private Quaternion deathViewRotation;

    private float mouseX, mouseY;

    void Start()
    {
        if (playerTransform == null)
            playerTransform = GameObject.Find("Player").transform;
    }

    void Update()
    {
        HandleMouseRotation();
        isAiming = Input.GetMouseButton(1); // 우클릭으로 조준
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        if (!isDeathView)
        {
            Vector3 targetPosition = playerTransform.position + (isAiming ? aimOffset : offset);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * aimTransitionSpeed);
            transform.LookAt(playerTransform);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, deathViewRotation, Time.deltaTime);
        }
    }

    void HandleMouseRotation()
    {
        if (isDeathView) return;

        float rotSpeed = isAiming ? aimSensitivity : rotationSpeed;

        mouseX += Input.GetAxis("Mouse X") * rotSpeed;
        mouseY -= Input.GetAxis("Mouse Y") * rotSpeed;
        mouseY = Mathf.Clamp(mouseY, -30f, 60f);

        transform.rotation = Quaternion.Euler(mouseY, mouseX, 0);
    }

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
