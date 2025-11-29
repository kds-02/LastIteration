using UnityEngine;
using UnityEngine.UI;
using Api.Models;

public class AuthUI : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;

    [Header("Login UI")]
    [SerializeField] private InputField loginEmailInput;
    [SerializeField] private InputField loginPasswordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button goToRegisterButton;
    [SerializeField] private Text loginErrorText;

    [Header("Register UI")]
    [SerializeField] private InputField registerEmailInput;
    [SerializeField] private InputField registerPasswordInput;
    [SerializeField] private InputField registerConfirmPasswordInput;
    [SerializeField] private InputField registerNicknameInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button goToLoginButton;
    [SerializeField] private Text registerErrorText;

    private bool _isProcessing = false;

    private void Start()
    {
        ShowLoginPanel();

        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginClicked);
        if (goToRegisterButton != null)
            goToRegisterButton.onClick.AddListener(ShowRegisterPanel);
        if (registerButton != null)
            registerButton.onClick.AddListener(OnRegisterClicked);
        if (goToLoginButton != null)
            goToLoginButton.onClick.AddListener(ShowLoginPanel);

        ClearErrors();
    }

    public void ShowLoginPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
        ClearErrors();
    }

    public void ShowRegisterPanel()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
        ClearErrors();
    }

    private void ClearErrors()
    {
        if (loginErrorText != null) loginErrorText.text = "";
        if (registerErrorText != null) registerErrorText.text = "";
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (loginButton != null) loginButton.interactable = interactable;
        if (registerButton != null) registerButton.interactable = interactable;
        if (goToRegisterButton != null) goToRegisterButton.interactable = interactable;
        if (goToLoginButton != null) goToLoginButton.interactable = interactable;
    }

    private void OnLoginClicked()
    {
        if (_isProcessing) return;

        string email = loginEmailInput != null ? loginEmailInput.text.Trim() : "";
        string password = loginPasswordInput != null ? loginPasswordInput.text : "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowLoginError("이메일과 비밀번호를 입력해주세요.");
            return;
        }

        _isProcessing = true;
        SetButtonsInteractable(false);
        ShowLoginInfo("로그인 중...");

        AuthApiClient.Instance.Login(email, password, OnLoginSuccess, OnLoginFailed);
    }

    private void OnRegisterClicked()
    {
        if (_isProcessing) return;

        string email = registerEmailInput != null ? registerEmailInput.text.Trim() : "";
        string password = registerPasswordInput != null ? registerPasswordInput.text : "";
        string confirmPassword = registerConfirmPasswordInput != null ? registerConfirmPasswordInput.text : "";
        string nickname = registerNicknameInput != null ? registerNicknameInput.text.Trim() : "";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(nickname))
        {
            ShowRegisterError("모든 필드를 입력해주세요.");
            return;
        }

        if (password != confirmPassword)
        {
            ShowRegisterError("비밀번호가 일치하지 않습니다.");
            return;
        }

        if (password.Length < 8)
        {
            ShowRegisterError("비밀번호는 8자 이상이어야 합니다.");
            return;
        }

        _isProcessing = true;
        SetButtonsInteractable(false);
        ShowRegisterInfo("회원가입 중...");

        AuthApiClient.Instance.Register(email, nickname, password, OnRegisterSuccess, OnRegisterFailed);
    }

    private void OnLoginSuccess(LoginData data)
    {
        Debug.Log($"[AuthUI] 로그인 성공! user_id: {data.user_id}");

        AuthManager.SaveLoginInfo(data.session_id, data.user_id, data.email);

        ShowLoginInfo("사용자 정보 불러오는 중...");
        AuthApiClient.Instance.GetMe(
            onSuccess: (meData) =>
            {
                _isProcessing = false;
                SetButtonsInteractable(true);

                Debug.Log($"[AuthUI] 사용자 정보 로드 완료! nickname: {meData.nickname}");
                AuthManager.UpdateUserInfo(meData.user_id, meData.email, meData.nickname);
                AuthManager.GoToMenu();
            },
            onError: (error) =>
            {
                _isProcessing = false;
                SetButtonsInteractable(true);

                Debug.LogWarning($"[AuthUI] 사용자 정보 로드 실패: {error}, 메뉴로 이동");
                AuthManager.GoToMenu();
            }
        );
    }

    private void OnLoginFailed(string error)
    {
        _isProcessing = false;
        SetButtonsInteractable(true);
        ShowLoginError(error);
    }

    private void OnRegisterSuccess(RegisterData data)
    {
        _isProcessing = false;
        SetButtonsInteractable(true);

        Debug.Log($"[AuthUI] 회원가입 성공! user_id: {data.user_id}, nickname: {data.nickname}");

        ShowLoginPanel();
        ShowLoginSuccess("회원가입이 완료되었습니다. 로그인해주세요.");
    }

    private void OnRegisterFailed(string error)
    {
        _isProcessing = false;
        SetButtonsInteractable(true);
        ShowRegisterError(error);
    }

    private void ShowLoginError(string message)
    {
        if (loginErrorText != null)
        {
            loginErrorText.color = Color.red;
            loginErrorText.text = message;
        }
    }

    private void ShowLoginSuccess(string message)
    {
        if (loginErrorText != null)
        {
            loginErrorText.color = Color.green;
            loginErrorText.text = message;
        }
    }

    private void ShowLoginInfo(string message)
    {
        if (loginErrorText != null)
        {
            loginErrorText.color = Color.white;
            loginErrorText.text = message;
        }
    }

    private void ShowRegisterError(string message)
    {
        if (registerErrorText != null)
        {
            registerErrorText.color = Color.red;
            registerErrorText.text = message;
        }
    }

    private void ShowRegisterInfo(string message)
    {
        if (registerErrorText != null)
        {
            registerErrorText.color = Color.white;
            registerErrorText.text = message;
        }
    }
}
