using UnityEngine;
using UnityEngine.UI;

public class HUDKillDeathUI : MonoBehaviour
{
    private PlayerState player;       // 자동으로 찾을 플레이어 상태
    private Text kdText;              // K/D 표시용
    private Text respawnText;         // 리스폰 시간 표시용

    void Start()
    {
        // PlayerState 자동 탐색
        player = FindObjectOfType<PlayerState>();

        // 자식 Text 컴포넌트를 자동으로 탐색
        Text[] texts = GetComponentsInChildren<Text>(true);
        foreach (var t in texts)
        {
            if (t.name.ToLower().Contains("kd"))
                kdText = t;
            else if (t.name.ToLower().Contains("respawn"))
                respawnText = t;
        }

        // 초기화
        if (kdText != null) kdText.text = "K 0 / D 0";
        if (respawnText != null)
        {
            respawnText.text = "";
            respawnText.enabled = false;
        }
    }

    void Update()
    {
        if (player == null) return;

        // --- K/D 갱신 ---
        if (kdText != null)
        {
            kdText.text = $"K {player.GetKill():0} / D {player.GetDeath():0}";
        }

        // --- 사망 시 리스폰 남은 시간 ---
        if (respawnText != null)
        {
            if (player.IsDead())
            {
                float remain = Mathf.Ceil(player.GetRespawnRemaining());
                respawnText.enabled = true;
                respawnText.text = $"Wating for Respawn {remain:0}";
            }
            else
            {
                respawnText.enabled = false;
                respawnText.text = "";
            }
        }
    }
}
