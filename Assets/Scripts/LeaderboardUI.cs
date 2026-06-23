using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private Transform leaderboardListParent;

    [SerializeField]
    private GameObject leaderboardEntryPrefab;

    [SerializeField]
    private Button refreshButton;

    [SerializeField]
    private Toggle realtimeToggle;

    [SerializeField]
    private TextMeshProUGUI statusText;

    [SerializeField]
    private Button closeButton;

    [Header("Settings")]
    [SerializeField]
    private int topCount = 10;

    private bool isRealtimeEnabled = false;

    private async UniTaskVoid Start()
    {
        

        refreshButton.onClick.AddListener(() => LoadAndDisplayLeaderboardAsync().Forget());
        realtimeToggle.onValueChanged.AddListener(OnRealtimeToggleChanged);
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        LeaderboardManager.Instance.OnLeaderboardUpdated += OnLeaderboardUpdated;

        isRealtimeEnabled = true;
        statusText.text = "실시간 동기화 중 ...";
        LeaderboardManager.Instance.StartRealtimeListener();
    }

    private async UniTaskVoid LoadAndDisplayLeaderboardAsync()
    {
        statusText.text = "실시간 동기화 중..." ;

        List<LeaderboardEntry> leaderboard = 
            await LeaderboardManager.Instance.LoadLeaderboardAsync(topCount);

        DisplayLeaderboard(leaderboard);

        statusText.text = $"상위 {leaderboard.Count}명";
    }

    private void DisplayLeaderboard(List<LeaderboardEntry> leaderboard)
    {
        foreach (Transform child in leaderboardListParent)
        {
            Destroy(child.gameObject);
        }

        int rank = 1;
        foreach (LeaderboardEntry entry in leaderboard)
        {
            GameObject item = Instantiate(leaderboardEntryPrefab, leaderboardListParent);

            TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();

            if (texts.Length >= 3)
            {
                texts[0].text = $"{rank}";
                texts[1].text = entry.nickname;
                texts[2].text = $"{entry.score}점";
            }

            rank++;
        }

        Debug.Log($"[LeaderboardUI] 리더보드 표시 완료: {leaderboard.Count}명");
    }

    private void OnRealtimeToggleChanged(bool isOn)
    {
        if (isOn)
        {
            LeaderboardManager.Instance.StartRealtimeListener(topCount);
            statusText.text = $"실시간 동기화 중..."    ;
            isRealtimeEnabled = true;
        }
        else
        {
            LeaderboardManager.Instance.StopRealtimeListener();
            statusText.text = $"실시간 동기화 꺼짐..."    ;
            isRealtimeEnabled = false;
        }
    }

    private void OnLeaderboardUpdated(List<LeaderboardEntry> leaderboard)
    {
        Debug.Log("[LeaderboardUI] 실시간 업데이트 수신");
        DisplayLeaderboard(leaderboard);
        statusText.text = $"업데이트됨 ({leaderboard.Count}명)";
    }

    private void OnDestroy()
    {
        LeaderboardManager.Instance.OnLeaderboardUpdated -= OnLeaderboardUpdated;
        LeaderboardManager.Instance.StopRealtimeListener();
    }
}
