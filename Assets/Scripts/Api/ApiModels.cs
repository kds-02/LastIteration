using System;

namespace Api.Models
{
    [Serializable]
    public class LoginRequest
    {
        public string email;
        public string password;

        public LoginRequest(string email, string password)
        {
            this.email = email;
            this.password = password;
        }
    }

    [Serializable]
    public class RegisterRequest
    {
        public string email;
        public string nickname;
        public string password;

        public RegisterRequest(string email, string nickname, string password)
        {
            this.email = email;
            this.nickname = nickname;
            this.password = password;
        }
    }

    [Serializable]
    public class ApiResponse<T>
    {
        public bool success;
        public T data;
        public string timestamp;
    }

    [Serializable]
    public class ApiErrorResponse
    {
        public bool success;
        public string code;
        public string message;
        public string timestamp;
    }

    [Serializable]
    public class LoginData
    {
        public string session_id;
        public string user_id;
        public string email;
    }

    [Serializable]
    public class RegisterData
    {
        public string user_id;
        public string email;
        public string nickname;
    }

    [Serializable]
    public class LogoutData
    {
        public string message;
    }

    [Serializable]
    public class MeData
    {
        public string user_id;
        public string email;
        public string nickname;
    }

    [Serializable]
    public class LoginResponse : ApiResponse<LoginData> { }

    [Serializable]
    public class RegisterResponse : ApiResponse<RegisterData> { }

    [Serializable]
    public class LogoutResponse : ApiResponse<LogoutData> { }

    [Serializable]
    public class MeResponse : ApiResponse<MeData> { }
}
