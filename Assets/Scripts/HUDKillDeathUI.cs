using UnityEngine;
using UnityEngine.UI;
using Fusion;                     // ★ Fusion 추가

public class HUDKillDeathUI : MonoBehaviour
{
    private PlayerState player;   // 로컬 플레이어의 PlayerState
    private Text kdText;          // K/D 표시용
    private Text respawnText;     // 리스폰 시간 표시용

    void Start()
    {
        // 자식 Text 컴포넌트를 자동으로 탐색
        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (var t in texts)
        {
            var nameLower = t.name.ToLower();
            if (nameLower.Contains("kd"))
                kdText = t;
            else if (nameLower.Contains("respawn"))
                respawnText = t;
        }

        // 초기화
        if (kdText != null) kdText.text = "0          0";
        if (respawnText != null)
        {
            respawnText.text = "";
            respawnText.enabled = false;
        }
    }

    void Update()
    {
        // ★ 아직 로컬 플레이어를 못 찾았으면, 매 프레임 찾아보기
        if (player == null)
        {
            var allPlayers = FindObjectsOfType<PlayerState>();
            foreach (var p in allPlayers)
            {
                // NetworkObject가 있고, 입력 권한(로컬 플레이어) 가진 객체만 HUD에 연결
                if (p.Object != null && p.Object.HasInputAuthority)
                {
                    player = p;
                    break;
                }
            }

            // 여전히 못 찾았으면 다음 프레임에 다시 시도
            if (player == null) return;
        }

        // --- K/D 업데이트 (서버에서 Sync된 Networked 값 읽기) ---
        if (kdText != null)
        {
            kdText.text = $"{player.GetKill():0}          {player.GetDeath():0}";
        }

        // --- 사망 시 리스폰 남은 시간 (서버 TickTimer 기반) ---
        if (respawnText != null)
        {
            // NetworkBool → bool (암시적 변환 가능)
            if (player.IsDead)
            {
                float remain = Mathf.Ceil(player.GetRespawnRemaining());
                respawnText.enabled = true;
                respawnText.text = $"Waiting for Respawn {remain:0}";
            }
            else
            {
                respawnText.enabled = false;
                respawnText.text = "";
            }
        }
    }
}
