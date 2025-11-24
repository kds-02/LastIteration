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

    private const int KillLimit = 10;

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

            // 종료처리 분기
            if(GameStarted && !GameEnded)
            {
                var topPlayer = FindTopPlayer();

                // 킬 제한 달성 시 즉시 종료
                if (topPlayer != null && topPlayer.GetKill() >= KillLimit)
                {
                    GameEnded = true;
                    RPC_EndGame(topPlayer.Object.InputAuthority.RawEncoded, topPlayer.GetKill(), topPlayer.GetDeath(), true);
                }
                // 타이머가 끝났다면 종료 처리
                else if (Runner.SimulationTime >= GameEndTime)
                {
                    GameEnded = true;
                    if (topPlayer != null)
                    {
                        RPC_EndGame(topPlayer.Object.InputAuthority.RawEncoded, topPlayer.GetKill(), topPlayer.GetDeath(), false);
                    }
                    else
                    {
                        RPC_EndGame(-1, 0f, 0f, false);
                    }
                }
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
    private void RPC_EndGame(int winnerRawId, float winnerKill, float winnerDeath, bool killLimitReached)
    {
        Debug.Log("[GameManager] 게임 종료 - EndScene 로드");
        if (endgameText != null)
        {
            string winnerName = ResolvePlayerName(winnerRawId);
            string reason = killLimitReached ? "Kill limit reached" : "Time is up";
            string winnerLine = winnerRawId >= 0
                ? $"Winner: {winnerName} (K:{winnerKill:0} / D:{winnerDeath:0})"
                : "Winner: N/A";
            endgameText.text = $"End Game!\n{reason}\n{winnerLine}";
        }
        SceneManager.LoadScene("EndScene");
    }

    private PlayerState FindTopPlayer()
    {
        PlayerState topPlayer = null;

        foreach (var player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out var playerObject))
                continue;

            var state = playerObject.GetComponent<PlayerState>();
            if (state == null)
                continue;

            if (topPlayer == null)
            {
                topPlayer = state;
                continue;
            }

            if (state.GetKill() > topPlayer.GetKill())
            {
                topPlayer = state;
            }
            else if (Mathf.Approximately(state.GetKill(), topPlayer.GetKill()) && state.GetDeath() < topPlayer.GetDeath())
            {
                topPlayer = state;
            }
        }

        return topPlayer;
    }

    private string ResolvePlayerName(int winnerRawId)
    {
        foreach (var player in Runner.ActivePlayers)
        {
            if (player.RawEncoded == winnerRawId)
                return player.ToString();
        }

        return winnerRawId >= 0 ? $"Player {winnerRawId}" : "Unknown";
    }
}
