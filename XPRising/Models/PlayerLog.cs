namespace XPRising.Models;

public struct PlayerLog
{
    public bool LoggingWanted = false;
    public bool LoggingExp = false;
    public bool LoggingMastery = false;
    public bool LoggingBloodline = false;

    public PlayerLog(bool loggingWanted, bool loggingExp, bool loggingMastery, bool loggingBloodline)
    {
        this.LoggingWanted = loggingWanted;
        this.LoggingExp = loggingExp;
        this.LoggingMastery = loggingMastery;
        this.LoggingBloodline = loggingBloodline;
    }
}