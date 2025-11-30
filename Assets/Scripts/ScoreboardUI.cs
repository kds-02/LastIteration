using System.Text;
using TMPro;
using UnityEngine;


public class ScoreboardUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;       // 전체 보드 활성화용
    [SerializeField] private TMP_Text bodyText;      // 내용 출력용
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    private void Update()
    {
        bool show = Input.GetKey(toggleKey);

        if (panel != null)
        {
            // 패널을 비활성화하면 이 스크립트 Update도 멈추므로,
            // 패널이 자기 자신이면 CanvasGroup으로 숨김 처리
            if (panel == gameObject)
            {
                var cg = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
                cg.alpha = show ? 1f : 0f;
                cg.interactable = show;
                cg.blocksRaycasts = show;
            }
            else
            {
                panel.SetActive(show);
            }
        }

        if (show)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (bodyText == null)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("Name  \tK\tD");

        var players = FindObjectsOfType<PlayerState>();
        if (players.Length == 0)
        {
            sb.AppendLine("No players in room");
            bodyText.text = sb.ToString();
            return;
        }

        System.Array.Sort(players, (a, b) =>
        {
            int ida = a.Object != null ? a.Object.InputAuthority.PlayerId : 0;
            int idb = b.Object != null ? b.Object.InputAuthority.PlayerId : 0;
            return ida.CompareTo(idb);
        });

        int idx = 1;
        foreach (var p in players)
        {
            string name = p.GetNickname();
            if (string.IsNullOrEmpty(name))
                name = $"Player{(p.Object != null ? p.Object.InputAuthority.PlayerId : -1)}";

            string line = $"{idx,2}. {name,-15} {p.GetKill(),2:0} / {p.GetDeath(),2:0}";
            sb.AppendLine(line);
            idx++;
        }

        bodyText.text = sb.ToString();
    }
}

