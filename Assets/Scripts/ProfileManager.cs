using System;
using Cysharp.Threading.Tasks;
using Firebase.Database;
using NUnit.Framework;
using UnityEngine;

public class ProfileManager : MonoBehaviour
{
    private static ProfileManager instance;
    public static ProfileManager Instance => instance;

    private DatabaseReference databaseRef;
    private DatabaseReference userRef;

    private UserProfile cachedProfile;
    public UserProfile CachedProfile => cachedProfile;

    private bool isInitialized;
    public bool IsInitialized => isInitialized;


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
            Debug.LogError("[Profile] 파이어 베이스 초기화 실패. 프로파일 초기화 불가.");
            return;
        }

        databaseRef = FirebaseInitializer.Instance.Database.RootReference;
        userRef = databaseRef.Child("users");

        await LoadProfileAsync();
        
        isInitialized = true;

        Debug.Log($"[Profile] 프로파일 초기화 완료.");
    }

    public async UniTask<(bool success, string error)> SaveProfileAsync(string nickname)
    {
        if (!AuthManager.Instance.IsLoggedIn)
        {
            return (false, "[Profile] 로그인이 필요합니다.");
        }

        string userId = AuthManager.Instance.UserId;
        string email = AuthManager.Instance.CurrentUser.Email ?? "익명";

        try
        {
            Debug.Log("[Profile] 프로필 저장 시도");
            
            UserProfile profile = new (nickname, email);
            string json = profile.ToJson();

            await userRef.Child(userId).SetRawJsonValueAsync(json);
            cachedProfile = profile;

            Debug.Log("[Profile] 프로필 저장 성공");
            return(true, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Profile] 프로필 저장 실패 : {ex.Message}");
            return (false, ex.Message);
        }
    }

    public async UniTask<(UserProfile profile, string error)> LoadProfileAsync()
    {
        if (!AuthManager.Instance.IsLoggedIn)
        {
            return (null, "[Profile] 로그인이 필요합니다.");
        }

        string userId = AuthManager.Instance.UserId;

        try
        {
            Debug.Log("[Profile] 프로필 불러오기 시도");

            DataSnapshot snapshot = await userRef.Child(userId).GetValueAsync();
            if (!snapshot.Exists)
            {
                Debug.Log($"[Profile] 프로필 없음.");
                return (null, "프로필이 존재하지 않습니다.");
            }

            string json = snapshot.GetRawJsonValue();
            UserProfile profile = UserProfile.FromJson(json);
            cachedProfile = profile;
            Debug.Log($"[Profile] 프로필 불러오기 성공 {profile.nickname}.");
            return(profile, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Profile] 프로필 저장 실패 : {ex.Message}");
            return (null, ex.Message);
        }
    }

    public async UniTask<(bool success, string error)> UpdateNickNameAsync(string nickname)
    {
        if (!AuthManager.Instance.IsLoggedIn)
        {
            return (false, "[Profile] 로그인이 필요합니다.");
        }

        string userId = AuthManager.Instance.UserId;

        try
        {
            Debug.Log("[Profile] 닉네임 변경 시도");
            
            await userRef.Child(userId).Child("nickname").SetValueAsync(nickname);
            cachedProfile.nickname = nickname;

            Debug.Log("[Profile] 닉네임 변경 성공");
            return(true, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Profile] 닉네임 변경 실패 : {ex.Message}");
            return (false, ex.Message);
        }
    }
}
