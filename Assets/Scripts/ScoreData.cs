using System;

[Serializable]
public class ScoreData
{
    public int score;
    public long timestamp;

    public ScoreData() { }

    public ScoreData(int score, long timestamp)
    {
        this.score = score;
        this.timestamp = timestamp;
    }

    public DateTime GetDateTime()
    {
        return TimeUtil.FromUnixMillis(timestamp);
    }

    public string GetDateString()
    {
        return TimeUtil.ToDateString(timestamp);
    }
}
