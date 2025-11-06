using UnityEngine;

public class MockPlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(x, 0, z);
        transform.Translate(move * moveSpeed * Time.deltaTime);

        // 테스트용 사망/리스폰 입력
        if (Input.GetKeyDown(KeyCode.K))
            FindObjectOfType<CameraController>().OnPlayerDeath();

        if (Input.GetKeyDown(KeyCode.L))
            FindObjectOfType<CameraController>().OnPlayerRespawn();
    }
}
