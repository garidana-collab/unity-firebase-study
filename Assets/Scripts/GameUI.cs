using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("Game UI")]
    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI timeText;

    [Header("Start UI")]
    [SerializeField]
    private GameObject startPanel;

    [SerializeField]
    private Button startButton;

    [Header("Result UI")]
    [SerializeField]
    private GameObject resultPanel;

    [SerializeField]
    private TextMeshProUGUI finalScoreText;

    [SerializeField]
    private Button restartButton;

    [SerializeField]
    private Button resultHistoryButton;

    [SerializeField]
    private Button resultleaderboardButton;

    
    [SerializeField]
    private GameObject historyPanel;

    [SerializeField]
    private Button historyButton;

    [SerializeField]
    private GameObject leaderboardPanel;

    [SerializeField]
    private Button leaderboardButton;

    public event Action OnStartButtonClicked;
    public event Action OnRestartButtonClicked;

    private void Awake()
    {
        startButton.onClick.AddListener(() => OnStartButtonClicked?.Invoke());
        restartButton.onClick.AddListener(() => OnRestartButtonClicked?.Invoke());
        historyButton.onClick.AddListener(ShowHistory);
        resultHistoryButton.onClick.AddListener(ShowHistory);
        leaderboardButton.onClick.AddListener(() => leaderboardPanel.SetActive(true));
        resultleaderboardButton.onClick.AddListener(() => leaderboardPanel.SetActive(true));
    }

    public void UpdateScore(int score)
    {
        scoreText.text = $"Score: {score}";
    }

    public void UpdateTime(float time)
    {
        int seconds = Mathf.CeilToInt(time);
        timeText.text = $"Time: {seconds}";
    }

    public void ShowGameUI()
    {
        startPanel.SetActive(false);
        resultPanel.SetActive(false);
        scoreText.gameObject.SetActive(true);
        timeText.gameObject.SetActive(true);
    }

    public void ShowResult(int finalScore)
    {
        scoreText.gameObject.SetActive(false);
        timeText.gameObject.SetActive(false);
        resultPanel.SetActive(true);
        finalScoreText.text = $"점수: {finalScore}";
    }

    public void ShowStartScreen()
    {
        startPanel.SetActive(true);
        resultPanel.SetActive(false);
        scoreText.gameObject.SetActive(false);
        timeText.gameObject.SetActive(false);
    }

    public void ShowHistory()
    {
        historyPanel.SetActive(true);
    }
}
