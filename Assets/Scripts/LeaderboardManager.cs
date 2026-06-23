using System;
using System.Collections.Generic;
using Firebase.Database;
using UnityEngine;
using Cysharp.Threading.Tasks;
using NUnit.Framework.Constraints;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.Rendering;
using Cysharp.Threading.Tasks.CompilerServices;
using System.ComponentModel;


public class LeaderboardManager : MonoBehaviour
{
    private static LeaderboardManager instance;
    public static LeaderboardManager Instance => instance;


    private DatabaseReference leaderboardRef;
    private Query listenerQuery;

    private bool isListenerActive;

    public event Action<List<LeaderboardEntry>> OnLeaderboardUpdated;

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
            Debug.LogError($"[Leaderboard] Firebase 초기화 실패.");
        }

        leaderboardRef = FirebaseInitializer.Instance.Database.RootReference.Child("leaderboard");
    }

    private void OnDestroy()
    {
        StopRealtimeListener();
    }

    public async UniTask<(bool success, string error)> SaveToLeaderboard( int score )
    {
        if (!AuthManager.Instance.IsLoggedIn)
        {
            return (false, $"로그인이 필요합니다.");
        }

        if (leaderboardRef == null)
        {
            return (false, $"leaderboarRef 널");
        }

        string userId = AuthManager.Instance.UserId;
        string nickName = ProfileManager.Instance.CachedProfile.nickname ?? $"익명";

        try
        {
            Debug.Log($"[Leaderboard] 시도");

            Dictionary<string, object> entryData = new()
            {
                {"userId", userId},
                {"nickname", nickName},
                {"score", score},
                {"timestamp", ServerValue.Timestamp}
            };

            await leaderboardRef.Child(userId).UpdateChildrenAsync(entryData);

            Debug.Log($"[Leaderboard] 성공");   
            return(true, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Leaderboard] 저장 실패 : {ex.Message}");
            return (false, "");
        }
    } 

    public async UniTask<List<LeaderboardEntry>> LoadLeaderboardAsync (int limit = 10)
    {
        if (leaderboardRef == null)
        {
            return new();
        }

        try
        {

            Debug.Log($"[Leaderboard] 로드 시도");
            Query query = leaderboardRef.OrderByChild("score").LimitToLast(limit);
            DataSnapshot snapshot = await query.GetValueAsync();

            List<LeaderboardEntry> leaderboard = ParseEntries(snapshot);

            Debug.Log($"[Leaderboard] 로드 성공");   
            return leaderboard;
        }
        catch(Exception ex)
        {
            Debug.LogError($"[Leaderboard] 로드 실패 : {ex.Message}");
            return new();
        }
    }

    public List<LeaderboardEntry> ParseEntries (DataSnapshot snapshot)
    {
        List<LeaderboardEntry> list = new();

        if (snapshot.Exists)
        {
            foreach(DataSnapshot child in snapshot.Children)
            {
                list.Add(LeaderboardEntry.FromJson(child.GetRawJsonValue()));
            }
        }

        list.Sort((a, b) => b.score.CompareTo(a.score));
        
        return list;
    }

    public void StartRealtimeListener(int limit = 10)
    {
        if (isListenerActive || leaderboardRef == null)
        {
            return;
        }
        Debug.Log("[Leaderboard] 실시간 리스너 시작");
        listenerQuery = leaderboardRef.OrderByChild("score").LimitToLast(limit);
        listenerQuery.ValueChanged += OnValueChanged;
        isListenerActive = true;

        Debug.Log("[Leaderboard] 실시간 리스너 끝");
    }

    public void StopRealtimeListener()
    {
        if (isListenerActive && listenerQuery != null)
        {
            Debug.Log("[Leaderboard] 실시간 리스너 중지");
            listenerQuery.ValueChanged -= OnValueChanged;
            listenerQuery = null;
            isListenerActive = false;
        }
    }

    private void OnValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.Log ($"[Leaderboard] 리스너 오류 : {args.DatabaseError.Message}");
            return;
        }

        List<LeaderboardEntry> leaderboard = ParseEntries(args.Snapshot);
        DispatchUpdateAsync(leaderboard).Forget();
    }

    private async UniTaskVoid DispatchUpdateAsync(List<LeaderboardEntry> leaderboard)
    {
        await UniTask.SwitchToMainThread();

        OnLeaderboardUpdated?.Invoke(leaderboard);
    }
}
