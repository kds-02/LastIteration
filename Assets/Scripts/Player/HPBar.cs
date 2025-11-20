using UnityEngine;
using UnityEngine.UI;
using Fusion;

// 파일 이름: HPBar.cs
// 클래스 이름: HPBar (Unity 관례상 파일명이랑 맞추는 걸 추천)
public class HPBar : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;

    private PlayerState player;

    private void Awake()
    {
        // 이 오브젝트 자체에 Slider가 붙어있으면 자동으로 가져오기
        if (hpSlider == null)
        {
            hpSlider = GetComponent<Slider>();
        }

        if (hpSlider == null)
        {
            Debug.LogError("[HPBar] Slider 컴포넌트를 찾지 못했습니다.");
            return;
        }

        hpSlider.minValue = 0f;
        hpSlider.value = 0f;   // 초기 값
    }

    private void Update()
    {
        if (hpSlider == null)
            return;

        // 1) 아직 로컬 PlayerState를 못 찾았으면 한 번 찾기
        if (player == null)
        {
            var allPlayers = FindObjectsOfType<PlayerState>();
            foreach (var p in allPlayers)
            {
                if (p.Object != null && p.Object.HasInputAuthority)
                {
                    player = p;
                    Debug.Log("[HPBar] 로컬 PlayerState 연결 완료");
                    break;
                }
            }

            if (player == null)
                return;
        }

        // 2) HP 값 읽어서 슬라이더에 반영
        float hp = player.GetHp();
        float maxHp = player.GetMaxHp();

        if (maxHp <= 0f) return;

        if (!Mathf.Approximately(hpSlider.maxValue, maxHp))
        {
            hpSlider.maxValue = maxHp;
        }

        hpSlider.value = hp;
    }
}
