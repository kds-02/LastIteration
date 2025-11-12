using UnityEngine;

// 내 플레이어일 때만 카메라가 따라오게 설정
public class CameraFollow : MonoBehaviour
{
    private Transform target;
    public Vector3 offset = new Vector3(0f, 3f, -6f);
    public float followSpeed = 10f;

    public void SetTarget(Transform newTarget) // 타겟 설정 함수 (PlayerMovement에서 호출)
    {
        target = newTarget;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
        transform.LookAt(target);
    }
}
