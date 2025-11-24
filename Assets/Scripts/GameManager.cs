using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;


/// 네트워크로 공유되는 게임 타이머, 호스트가 시작 트리거를 쏘고
/// 모든 플레이어가 동일한 TickTimer를 기반으로 남은 시간을 볼 수 있게함

public class GameManager : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private Text endgameText;
    [SerializeField] private Text timeText;

    [Header("매치 설정")]
    [SerializeField] private float matchDurationSeconds = 300f; // 5분
    [SerializeField] private int minPlayersToStart = 2; // 이 인원 이상이면 자동 시작 //TODO: 서비스에 맞게 인원 수정(현재 테스트용 2)

    [Networked] private float GameEndTime { get; set; }     // Runner.SimulationTime 기준 종료 시각
    [Networked] private NetworkBool GameStarted { get; set; }
    [Networked] private NetworkBool GameEnded { get; set; }

    public override void FixedUpdateNetwork()
    {
        // 호스트만 게임 시작/종료를 결정 (상태 권한 있는 플레이어만)
        if (Object.HasStateAuthority)
        {
            // 최소 인원 충족 시 시작
            if (!GameStarted && Runner.ActivePlayers.Count() >= minPlayersToStart)
            {
                StartGameCountdown();
            }

            // 타이머가 끝났다면 종료 처리
            if (GameStarted && !GameEnded && Runner.SimulationTime >= GameEndTime)
            {
                GameEnded = true;
                RPC_EndGame();
            }
        }

        // 로컬 UI 업데이트 (권한과 무관) - Authority일 때도 한 번 실행
        UpdateUITimer();
    }

    // Proxy에서도 UI가 갱신되도록 Render 훅에서 다시 호출
    public override void Render()
    {
        UpdateUITimer();
    }

    private void StartGameCountdown()
    {
        GameStarted = true;
        GameEnded = false;
        GameEndTime = Runner.SimulationTime + matchDurationSeconds;
        Debug.Log($"[GameManager] 게임 시작! {matchDurationSeconds}초 카운트다운 시작");
    }

    private void UpdateUITimer()
    {
        if (timeText == null)   return;

        if (!GameStarted || GameEnded)
        {
            timeText.text = GameStarted && GameEnded ? "0:00" : "--:--";
            return;
        }

        float remaining = Mathf.Max(0f, GameEndTime - Runner.SimulationTime);
        int min = Mathf.FloorToInt(remaining / 60f);
        int sec = Mathf.FloorToInt(remaining % 60f);
        timeText.text = $"{min:0}:{sec:00}";
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EndGame()
    {
        Debug.Log("[GameManager] 게임 종료 - EndScene 로드");
        if (endgameText != null)
        {
            endgameText.text = "Game Over";
        }
        SceneManager.LoadScene("EndScene");
    }
}
