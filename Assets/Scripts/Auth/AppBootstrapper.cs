using UnityEngine;
using UnityEngine.SceneManagement;
using Api.Models;

public class AppBootstrapper : MonoBehaviour
{
    [SerializeField] private float splashDelay = 0.5f;

    private void Start()
    {
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);

        if (splashDelay > 0)
        {
            Invoke(nameof(CheckAuthAndNavigate), splashDelay);
        }
        else
        {
            CheckAuthAndNavigate();
        }
    }

    private void CheckAuthAndNavigate()
    {
        if (AuthManager.HasToken())
        {
            Debug.Log("[AppBootstrapper] 저장된 세션 발견 → 유효성 검증 시작");
            ValidateSession();
        }
        else
        {
            Debug.Log("[AppBootstrapper] 저장된 세션 없음 → AuthScene으로 이동");
            SceneManager.LoadScene("AuthScene");
        }
    }

    private void ValidateSession()
    {
        AuthApiClient.Instance.GetMe(
            onSuccess: (data) =>
            {
                Debug.Log($"[AppBootstrapper] 세션 유효! user: {data.email}, nickname: {data.nickname}");
                AuthManager.UpdateUserInfo(data.user_id, data.email, data.nickname);
                SceneManager.LoadScene("menuScene");
            },
            onError: (error) =>
            {
                Debug.Log($"[AppBootstrapper] 세션 만료 또는 무효: {error} → AuthScene으로 이동");
                AuthManager.ClearAll();
                SceneManager.LoadScene("AuthScene");
            }
        );
    }
}
