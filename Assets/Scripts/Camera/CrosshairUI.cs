using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("참조")]
    public RectTransform up, down, left, right; // 십자선 4개
    public GameObject aimDot;                   // 빨간 점
    public CameraController cameraController;   // 카메라 스크립트 참조

    [Header("Crosshair 설정")]
    public float defaultGap = 20f;   // 십자선 간격
    public float moveSpeed = 10f;    // 부드럽게 이동

    void Update()
    {
        if (cameraController == null) return;

        bool isAiming = cameraController.isAiming;

        // 조준 여부에 따라 활성화 전환
        aimDot.SetActive(isAiming);             // 조준 중일 때 빨간 점 활성화
        up.gameObject.SetActive(!isAiming);
        down.gameObject.SetActive(!isAiming);
        left.gameObject.SetActive(!isAiming);
        right.gameObject.SetActive(!isAiming);

        // 십자선 위치 부드럽게 복귀
        // if (!isAiming)
        // {
        //     up.anchoredPosition = Vector2.Lerp(up.anchoredPosition, new Vector2(0, defaultGap), Time.deltaTime * moveSpeed);
        //     down.anchoredPosition = Vector2.Lerp(down.anchoredPosition, new Vector2(0, -defaultGap), Time.deltaTime * moveSpeed);
        //     left.anchoredPosition = Vector2.Lerp(left.anchoredPosition, new Vector2(-defaultGap, 0), Time.deltaTime * moveSpeed);
        //     right.anchoredPosition = Vector2.Lerp(right.anchoredPosition, new Vector2(defaultGap, 0), Time.deltaTime * moveSpeed);
        // }
    }
}
