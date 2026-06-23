using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance => instance;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject circlePrefab;

    [Header("UI References")]
    [SerializeField]
    private GameUI gameUI;

    private CircleController currentCircle;
    private int score = 0;
    private float remainingTime;
    private bool isPaused = false;

    private enum GameState
    {
        Ready,
        Playing,
        Ended,
    }

    private GameState currentState = GameState.Ready;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        gameUI.OnStartButtonClicked += StartGame;
        gameUI.OnRestartButtonClicked += RestartGame;
    }

    public void StartGame()
    {
        if (currentState == GameState.Playing)
            return;

        score = 0;
        remainingTime = Constants.GAME_TIME;
        currentState = GameState.Playing;

        gameUI.ShowGameUI();
        gameUI.UpdateScore(score);
        gameUI.UpdateTime(remainingTime);

        SpawnCircle();

        GameTimerAsync(this.destroyCancellationToken).Forget();
    }

    private void SpawnCircle()
    {
        GameObject circleObj = Instantiate(circlePrefab);
        currentCircle = circleObj.GetComponent<CircleController>();
        currentCircle.Initialize(OnCircleClicked);
    }

    private void OnCircleClicked()
    {
        if (currentState != GameState.Playing || isPaused)
            return;

        score += Constants.POINTS_PER_CLICK;
        gameUI.UpdateScore(score);
    }

    private async UniTaskVoid GameTimerAsync(CancellationToken token)
    {
        while (remainingTime > 0 && !token.IsCancellationRequested)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(1f), ignoreTimeScale: false, cancellationToken: token);

            if (!isPaused)
            {
                remainingTime -= 1f;
                gameUI.UpdateTime(remainingTime);
            }
        }

        if (!token.IsCancellationRequested)
        {
            EndGame();
        }
    }

    private void EndGame()
    {
        if (currentState != GameState.Playing)
        {
            Debug.Log("[Game] 게임이 Playing 상태가 아니므로 종료하지 않음");
            return;
        }

        currentState = GameState.Ended;

        if (currentCircle != null)
        {
            Destroy(currentCircle.gameObject);
        }

        GameEvents.RaiseGameEnded(score);

        gameUI.ShowResult(score);
    }

    private void RestartGame()
    {
        StartGame();
    }

    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            isPaused = true;

            if (currentCircle != null)
            {
                currentCircle.Pause();
            }

            Debug.Log("[Game] 일시정지");
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Playing && isPaused)
        {
            isPaused = false;

            if (currentCircle != null)
            {
                currentCircle.Resume();
            }

            Debug.Log("[Game] 재개");
        }
    }

    public void ResetGame()
    {
        Debug.Log("[Game] 초기화");

        if (currentCircle != null)
        {
            Destroy(currentCircle.gameObject);
            currentCircle = null;
        }

        score = 0;
        remainingTime = 0;
        isPaused = false;
        currentState = GameState.Ready;

        GameEvents.RaiseGameReset();

        gameUI.ShowStartScreen();
    }

    private void OnDestroy()
    {
        gameUI.OnStartButtonClicked -= StartGame;
        gameUI.OnRestartButtonClicked -= RestartGame;
    }
}
