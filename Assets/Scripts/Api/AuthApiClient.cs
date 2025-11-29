using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Api.Models;

public class AuthApiClient : MonoBehaviour
{
    private static AuthApiClient _instance;
    public static AuthApiClient Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("AuthApiClient");
                _instance = go.AddComponent<AuthApiClient>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Login(string email, string password, Action<LoginData> onSuccess, Action<string> onError)
    {
        var request = new LoginRequest(email, password);
        StartCoroutine(PostRequest<LoginResponse, LoginData>(
            ApiConfig.Instance.LoginUrl,
            JsonUtility.ToJson(request),
            null,
            onSuccess,
            onError
        ));
    }

    public void Register(string email, string nickname, string password, Action<RegisterData> onSuccess, Action<string> onError)
    {
        var request = new RegisterRequest(email, nickname, password);
        StartCoroutine(PostRequest<RegisterResponse, RegisterData>(
            ApiConfig.Instance.RegisterUrl,
            JsonUtility.ToJson(request),
            null,
            onSuccess,
            onError
        ));
    }

    public void Logout(Action<LogoutData> onSuccess, Action<string> onError)
    {
        string token = AuthManager.GetToken();
        StartCoroutine(PostRequest<LogoutResponse, LogoutData>(
            ApiConfig.Instance.LogoutUrl,
            "{}",
            token,
            onSuccess,
            onError
        ));
    }

    public void GetMe(Action<MeData> onSuccess, Action<string> onError)
    {
        string token = AuthManager.GetToken();
        StartCoroutine(GetRequest<MeResponse, MeData>(
            ApiConfig.Instance.MeUrl,
            token,
            onSuccess,
            onError
        ));
    }

    private IEnumerator PostRequest<TResponse, TData>(
        string url,
        string jsonBody,
        string bearerToken,
        Action<TData> onSuccess,
        Action<string> onError)
        where TResponse : ApiResponse<TData>
    {
        using (var request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(bearerToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
            }

            request.timeout = ApiConfig.Instance.TimeoutSeconds;

            yield return request.SendWebRequest();

            HandleResponse<TResponse, TData>(request, onSuccess, onError);
        }
    }

    private IEnumerator GetRequest<TResponse, TData>(
        string url,
        string bearerToken,
        Action<TData> onSuccess,
        Action<string> onError)
        where TResponse : ApiResponse<TData>
    {
        using (var request = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(bearerToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
            }

            request.timeout = ApiConfig.Instance.TimeoutSeconds;

            yield return request.SendWebRequest();

            HandleResponse<TResponse, TData>(request, onSuccess, onError);
        }
    }

    private void HandleResponse<TResponse, TData>(
        UnityWebRequest request,
        Action<TData> onSuccess,
        Action<string> onError)
        where TResponse : ApiResponse<TData>
    {
        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"[AuthApiClient] 네트워크 에러: {request.error}");
            onError?.Invoke("서버에 연결할 수 없습니다.");
            return;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log($"[AuthApiClient] 응답: {responseText}");

        if (string.IsNullOrEmpty(responseText))
        {
            onError?.Invoke("서버 응답이 비어있습니다.");
            return;
        }

        try
        {
            var errorCheck = JsonUtility.FromJson<ApiErrorResponse>(responseText);

            if (!errorCheck.success)
            {
                string errorMessage = !string.IsNullOrEmpty(errorCheck.message)
                    ? errorCheck.message
                    : "알 수 없는 오류가 발생했습니다.";

                Debug.LogWarning($"[AuthApiClient] API 실패: {errorCheck.code} - {errorMessage}");
                onError?.Invoke(errorMessage);
                return;
            }

            var response = JsonUtility.FromJson<TResponse>(responseText);

            if (response.data != null)
            {
                onSuccess?.Invoke(response.data);
            }
            else
            {
                onError?.Invoke("응답 데이터가 비어있습니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AuthApiClient] JSON 파싱 에러: {e.Message}");
            onError?.Invoke("응답 처리 중 오류가 발생했습니다.");
        }
    }
}
