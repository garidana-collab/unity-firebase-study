using System;

public static class GameEvents
{
    public static event Action<int> GameEnded;

    public static event Action GameReset;

    public static void RaiseGameEnded(int finalScore) => GameEnded?.Invoke(finalScore);

    public static void RaiseGameReset() => GameReset?.Invoke();
}
