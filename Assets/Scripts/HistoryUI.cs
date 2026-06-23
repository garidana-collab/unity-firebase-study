using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HistoryUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI bestScoreText;

    [SerializeField]
    private Transform historyListParent;

    [SerializeField]
    private GameObject historyItemPrefab;

    [SerializeField]
    private Button refreshButton;

    [SerializeField]
    private Button closeButton;

    private void Start()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        }
    }

    private void OnEnable()
    {
        LoadAndDisplayHistoryAsync().Forget();
    }

    private void OnRefreshButtonClicked()
    {
        LoadAndDisplayHistoryAsync().Forget();
    }

    private async UniTask LoadAndDisplayHistoryAsync()
    {
        int bestscore = await ScoreManager.Instance.LoadBestScoreAsync();
        bestScoreText.text = $"최고 기록 : {bestscore}";

        List<ScoreData> history = await ScoreManager.Instance.LoadHistoryAsync();

        foreach (Transform child in historyListParent)
        {
            Destroy(child.gameObject);
        }

        if (history.Count == 0)
        {
            GameObject emptyItem = Instantiate(historyItemPrefab, historyListParent);
            TextMeshProUGUI text = emptyItem.GetComponentInChildren<TextMeshProUGUI>();
            text.text = $"게임 기록이 없습니다.";
        }
        else
        {
            foreach (ScoreData data in history)
            {
                GameObject item = Instantiate(historyItemPrefab, historyListParent);
                TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();
                text.text = $"{data.score}점 {data.GetDateString()}";
            }
        }
        Debug.Log($"[history UI] 히스토리 표시 완료 : {history.Count}");
    }
}
