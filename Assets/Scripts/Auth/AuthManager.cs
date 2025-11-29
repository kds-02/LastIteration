using UnityEngine;
using UnityEngine.SceneManagement;

public static class AuthManager
{
    private const string TOKEN_KEY = "auth_session_id";
    private const string USER_ID_KEY = "auth_user_id";
    private const string EMAIL_KEY = "auth_email";
    private const string NICKNAME_KEY = "auth_nickname";

    private const string AUTH_SCENE = "AuthScene";
    private const string MENU_SCENE = "menuScene";

    public static bool HasToken()
    {
        string token = PlayerPrefs.GetString(TOKEN_KEY, "");
        return !string.IsNullOrEmpty(token);
    }

    public static string GetToken()
    {
        return PlayerPrefs.GetString(TOKEN_KEY, "");
    }

    public static void SaveToken(string token)
    {
        PlayerPrefs.SetString(TOKEN_KEY, token);
        PlayerPrefs.Save();
        Debug.Log("[AuthManager] 세션 ID 저장 완료");
    }

    public static void SaveUserId(string userId)
    {
        PlayerPrefs.SetString(USER_ID_KEY, userId);
        PlayerPrefs.Save();
    }

    public static string GetUserId()
    {
        return PlayerPrefs.GetString(USER_ID_KEY, "");
    }

    public static void SaveEmail(string email)
    {
        PlayerPrefs.SetString(EMAIL_KEY, email);
        PlayerPrefs.Save();
    }

    public static string GetEmail()
    {
        return PlayerPrefs.GetString(EMAIL_KEY, "");
    }

    public static void SaveNickname(string nickname)
    {
        PlayerPrefs.SetString(NICKNAME_KEY, nickname);
        PlayerPrefs.Save();
    }

    public static string GetNickname()
    {
        return PlayerPrefs.GetString(NICKNAME_KEY, "");
    }

    public static void SaveLoginInfo(string sessionId, string userId, string email)
    {
        PlayerPrefs.SetString(TOKEN_KEY, sessionId);
        PlayerPrefs.SetString(USER_ID_KEY, userId);
        PlayerPrefs.SetString(EMAIL_KEY, email);
        PlayerPrefs.Save();
        Debug.Log($"[AuthManager] 로그인 정보 저장 완료 - userId: {userId}, email: {email}");
    }

    public static void UpdateUserInfo(string userId, string email, string nickname)
    {
        PlayerPrefs.SetString(USER_ID_KEY, userId);
        PlayerPrefs.SetString(EMAIL_KEY, email);
        PlayerPrefs.SetString(NICKNAME_KEY, nickname);
        PlayerPrefs.Save();
        Debug.Log($"[AuthManager] 사용자 정보 업데이트 - nickname: {nickname}");
    }

    public static void ClearAll()
    {
        PlayerPrefs.DeleteKey(TOKEN_KEY);
        PlayerPrefs.DeleteKey(USER_ID_KEY);
        PlayerPrefs.DeleteKey(EMAIL_KEY);
        PlayerPrefs.DeleteKey(NICKNAME_KEY);
        PlayerPrefs.Save();
        Debug.Log("[AuthManager] 모든 인증 정보 삭제 완료");
    }

    public static void Logout()
    {
        ClearAll();
        SceneManager.LoadScene(AUTH_SCENE);
    }

    public static void GoToMenu()
    {
        SceneManager.LoadScene(MENU_SCENE);
    }

    public static void GoToAuth()
    {
        SceneManager.LoadScene(AUTH_SCENE);
    }
}
