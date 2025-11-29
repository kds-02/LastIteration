using UnityEngine;
using UnityEngine.UI;

public class UserInfoUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text nicknameText;
    [SerializeField] private Button logoutButton;

    private void Start()
    {
        UpdateNicknameDisplay();

        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogoutClicked);
        }
    }

    private void UpdateNicknameDisplay()
    {
        if (nicknameText != null)
        {
            string nickname = AuthManager.GetNickname();
            nicknameText.text = string.IsNullOrEmpty(nickname) ? "Guest" : nickname;
        }
    }

    private void OnLogoutClicked()
    {
        Debug.Log("[UserInfoUI] 로그아웃 클릭");

        AuthApiClient.Instance.Logout(
            onSuccess: (data) =>
            {
                Debug.Log($"[UserInfoUI] 서버 로그아웃 성공: {data.message}");
                AuthManager.Logout();
            },
            onError: (error) =>
            {
                Debug.LogWarning($"[UserInfoUI] 서버 로그아웃 실패: {error}, 로컬 로그아웃 진행");
                AuthManager.Logout();
            }
        );
    }
}
