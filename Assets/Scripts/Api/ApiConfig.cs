using UnityEngine;

[CreateAssetMenu(fileName = "ApiConfig", menuName = "Config/ApiConfig")]
public class ApiConfig : ScriptableObject
{
    [Header("Server Settings")]
    [SerializeField] private string baseUrl = "http://127.0.0.1:3000";

    [Header("Timeout Settings")]
    [SerializeField] private int timeoutSeconds = 30;

    public string BaseUrl => baseUrl;
    public int TimeoutSeconds => timeoutSeconds;

    private static ApiConfig _instance;
    public static ApiConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ApiConfig>("ApiConfig");
                if (_instance == null)
                {
                    Debug.LogWarning("[ApiConfig] Resources/ApiConfig.asset을 찾을 수 없습니다. 기본값 사용.");
                    _instance = CreateInstance<ApiConfig>();
                }
            }
            return _instance;
        }
    }

    public string LoginUrl => $"{BaseUrl}/auth/login";
    public string RegisterUrl => $"{BaseUrl}/auth/register";
    public string LogoutUrl => $"{BaseUrl}/auth/logout";
    public string MeUrl => $"{BaseUrl}/auth/me";
}
