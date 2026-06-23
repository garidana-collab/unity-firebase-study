using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using Unity.Android.Gradle;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ScoreManager : MonoBehaviour
{
    private static ScoreManager instance;

    public static ScoreManager Instance => instance;

    private DatabaseReference scoresRef;

    private int cachedBestScore = 0;
    public int CachedBestScore => cachedBestScore;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private async UniTaskVoid Start()
    {
        if (!await FirebaseInitializer.Instance.WaitForInitializationAsync())
        {
            Debug.LogError("[Score] 파이어 베이스 초기화 X");
            return;
        }

        await UniTask.WaitUntil(() => AuthManager.Instance.IsInitialized);
        await UniTask.WaitUntil(() => ProfileManager.Instance.IsInitialized);


        scoresRef = FirebaseInitializer.Instance.Database.RootReference.Child("scores");
        Debug.Log("[Score] 파이어 베이스 초기화 완료");

        AuthManager.Instance.LoginStatusChanged += OnLoginStatusChanged;

        if (AuthManager.Instance.IsLoggedIn)
        {
            await SyncBestScoreToLeaderboard();
        }
    }

    private void OnLoginStatusChanged (bool signIn)
    {
        if (signIn)
        {
            SyncBestScoreToLeaderboard().Forget();
        }
        else
        {
            cachedBestScore = 0;
        }
    }

    private void OnDestroy()
    {
        AuthManager.Instance.LoginStatusChanged -= OnLoginStatusChanged;
    }

    private void OnEnable()
    {
        GameEvents.GameEnded += HandleGameEnded;
    }

    private void HandleGameEnded( int finalScore)
    {
        SaveScoreAsync(finalScore).Forget();
    }

    private async UniTask SyncBestScoreToLeaderboard()
    {
        int best = await LoadBestScoreAsync();
        if (best > 0 && LeaderboardManager.Instance != null)
        {
            await LeaderboardManager.Instance.SaveToLeaderboard(best);
            Debug.Log($"[Socre] 기존 최고 기록 {best} 점을 리더보드에 반영했습니다.");
        }
    }

    private void OnDisable()
    {
        GameEvents.GameEnded -= HandleGameEnded;
    }

    private async UniTask<(bool success, string error)> SaveScoreAsync(int score)
    {
        if (!AuthManager.Instance.IsLoggedIn)
        {
            return (false, "로그인 필요");
        }

        string userId = AuthManager.Instance.UserId;

        try
        {
            Debug.Log("[Score] 점수 저장 시도");

            DatabaseReference newHistoryRef = scoresRef.Child(userId).Child("history").Push();

            Dictionary<string, object> scoreData = new()
            {
                {"score", score},
                {"timestamp", ServerValue.Timestamp}
            };
            Debug.Log("1");
            await newHistoryRef.UpdateChildrenAsync(scoreData);

            if (score > cachedBestScore)
            {
                Debug.Log("2");
                await UpdateBestScoreAsync(score);
                Debug.Log("3");
                await LeaderboardManager.Instance.SaveToLeaderboard(score);
                
            }

            Debug.Log($"[Score] 점수 저장 성공 {score}");

            return (true, $"[Score] 점수 저장 성공 {score}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Score] 점수 저장 실패");
            return (false, ex.Message);
        }
    }

    private async UniTask UpdateBestScoreAsync(int newBestScore)
    {
        string userId = AuthManager.Instance.UserId;

        try
        {
            await scoresRef.Child(userId).Child("bestscore").SetValueAsync(newBestScore);
            cachedBestScore = newBestScore;
            Debug.Log($"[Score] 최고 기록 갱신 : {newBestScore}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Score] 최고 기록 갱신 실패");
        }
    }

    public async UniTask<int> LoadBestScoreAsync()
    {
        string userId = AuthManager.Instance.UserId;

        try
        {
            DataSnapshot snapshot = await scoresRef.Child(userId).Child("bestscore").GetValueAsync();
            cachedBestScore = snapshot.Exists ? FirebaseValue.ToInt(snapshot.Value) : 0;
            Debug.Log($"[Score] 최고 점수 로드 : {cachedBestScore}");
            return cachedBestScore;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Score] 최고 기록 로드 실패");
            return 0;
        }
    }

    public async UniTask<List<ScoreData>> LoadHistoryAsync(int limit = 10)
    {
        if (!AuthManager.Instance.IsLoggedIn || scoresRef == null)
        {
            Debug.Log($"Score : 로그인 필요");
            return new ();
        }

        string userId = AuthManager.Instance.UserId;

        try
        {
            Query query = scoresRef.Child(userId).Child("history")
                .OrderByChild("timestamp").LimitToLast(limit);

            // Query query = scoresRef.Child(userId).Child("history");


            DataSnapshot snapshot = await scoresRef.Child(userId).Child("history").GetValueAsync();

            List<ScoreData> historyList = new();
            if (snapshot.Exists)
            {
                foreach (DataSnapshot child in snapshot.Children)
                {
                    historyList.Add(JsonUtility.FromJson<ScoreData>(child.GetRawJsonValue()));
                }                
                historyList.Reverse();
            }

            return historyList;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Score] 히스토리 로드 실패");
            return new();
        }
    }
}
