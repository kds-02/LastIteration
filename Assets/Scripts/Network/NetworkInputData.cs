using UnityEngine;
using Fusion;

// Fusion에서 클라이언트 -> 서버로 전달되는 입력 데이터 구조체
public struct NetworkInputData : INetworkInput
{
    public Vector2 moveInput;   // 이동 입력 (WASD) - x: 좌우, y: 앞뒤
    public bool jumpPressed;    // 점프 눌림
    public bool runHeld;        // 달리기(Shift) 누르고 있는 중
    public bool crouchHeld;     // 앉기(Ctrl) 누르고 있는 중
}
