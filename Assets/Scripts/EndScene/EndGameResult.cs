public static class EndGameResult
{
    private static string reason = string.Empty;
    private static string winnerLine = string.Empty;

    public static void Set(string endReason, string winner)
    {
        reason = endReason;
        winnerLine = winner;
    }

    public static string GetCombinedMessage()
    {
        return $"End Game!\n{reason}\n{winnerLine}";
    }

    public static (string reason, string winnerLine) GetRawMessage()
    {
        return (reason, winnerLine);
    }
}