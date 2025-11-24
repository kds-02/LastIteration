using UnityEngine;
using UnityEngine.UI;

public class EndSceneUI : MonoBehaviour
{
    [SerializeField] private Text endTitleText;
    [SerializeField] private Text reasonText;
    [SerializeField] private Text winnerText;

    private void OnEnable()
    {
        UpdateLabels();
    }

    public void UpdateLabels()
    {
        var (reason, winnerLine) = EndGameResult.GetRawMessage();

        if (endTitleText != null)
            endTitleText.text = "End Game!";

        if (reasonText != null)
            reasonText.text = string.IsNullOrEmpty(reason) ? "" : reason;

        if (winnerText != null)
            winnerText.text = string.IsNullOrEmpty(winnerLine) ? "Winner: N/A" : winnerLine;
    }
}