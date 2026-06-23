# Firebase System Guide

이 문서는 현재 프로젝트의 Firebase 연동 구조를 다른 Unity 프로젝트에 옮길 수 있도록 정리한 설명서입니다.

현재 시스템은 Firebase Authentication과 Firebase Realtime Database를 사용합니다. 주요 기능은 로그인, 회원가입, 익명 로그인, 프로필 저장, 점수 저장, 점수 히스토리 조회, 최고 점수 저장, 리더보드 저장 및 실시간 갱신입니다.

## 사용 중인 Firebase 기능

- Firebase App
- Firebase Authentication
- Firebase Realtime Database

이 프로젝트에는 Firebase SDK 관련 파일이 `Assets/Firebase`, `Assets/Plugins` 아래에 들어 있고, 게임 로직에서 Firebase를 직접 사용하는 스크립트는 `Assets/Scripts` 아래에 있습니다.

## 전체 구동 흐름

Firebase 시스템은 다음 순서로 동작합니다.

1. `FirebaseInitializer`가 Firebase SDK 의존성을 확인하고 Firebase App, Auth, Database 인스턴스를 준비합니다.
2. `AuthManager`가 Firebase Auth를 받아 현재 로그인 상태를 확인하고 로그인/회원가입/로그아웃 기능을 제공합니다.
3. `ProfileManager`가 로그인된 유저의 프로필을 Realtime Database에서 읽고 저장합니다.
4. `ScoreManager`가 게임 종료 이벤트를 받아 점수 히스토리와 최고 점수를 저장합니다.
5. `LeaderboardManager`가 유저의 최고 점수를 리더보드에 저장하고, 리더보드 데이터를 읽거나 실시간으로 감시합니다.
6. UI 스크립트들이 각 매니저를 호출해서 로그인 화면, 프로필 화면, 기록 화면, 리더보드 화면을 갱신합니다.

핵심은 모든 Firebase 사용 스크립트가 `FirebaseInitializer.Instance.WaitForInitializationAsync()`를 기다린 뒤 동작한다는 점입니다.

## 핵심 스크립트 역할

### FirebaseInitializer

파일: `Assets/Scripts/FirebaseInitializer.cs`

Firebase 시스템의 시작점입니다.

`Awake()`에서 싱글톤 인스턴스를 만들고 `DontDestroyOnLoad(gameObject)`로 씬 전환 후에도 유지합니다. 이후 `InitializeFirebaseAsync()`를 실행합니다.

초기화 과정은 다음과 같습니다.

1. `FirebaseApp.CheckAndFixDependenciesAsync()`로 Firebase SDK 의존성을 확인합니다.
2. 의존성 상태가 `DependencyStatus.Available`이 아니면 초기화 실패 상태로 전환합니다.
3. `FirebaseApp.DefaultInstance`를 가져옵니다.
4. `FirebaseDatabase` 인스턴스를 가져옵니다.
5. `FirebaseAuth` 인스턴스를 가져옵니다.
6. 초기화 상태를 `Ready`로 바꿉니다.

다른 스크립트는 `WaitForInitializationAsync()`를 호출해서 초기화가 끝날 때까지 기다립니다.

```csharp
bool isReady = await FirebaseInitializer.Instance.WaitForInitializationAsync();
if (!isReady)
{
    return;
}
```

이 구조 덕분에 Auth나 Database를 Firebase 초기화 전에 접근하는 문제를 줄일 수 있습니다.

### FirebaseConfig

파일: `Assets/Scripts/FirebaseConfig.cs`

Firebase 설정값을 담는 `ScriptableObject`입니다.

현재 프로젝트에서는 `Assets/Resources/FirebaseConfig.asset`으로 사용합니다. `FirebaseInitializer`는 `Resources.Load<FirebaseConfig>("FirebaseConfig")`로 이 파일을 불러옵니다.

주요 필드는 다음과 같습니다.

- `apiKey`
- `appId`
- `projectId`
- `databaseUrl`
- `storageBucket`

실제로 현재 코드에서 중요하게 쓰는 값은 `databaseUrl`입니다. 값이 있으면 다음 방식으로 Realtime Database 인스턴스를 생성합니다.

```csharp
FirebaseDatabase.GetInstance(app, config.databaseUrl);
```

값이 없으면 기본 Firebase App 설정을 기준으로 Database 인스턴스를 가져옵니다.

### AuthManager

파일: `Assets/Scripts/AuthManager.cs`

Firebase Authentication 담당 매니저입니다.

제공 기능은 다음과 같습니다.

- 현재 로그인 유저 보관
- 현재 유저 ID 제공
- 익명 로그인
- 이메일 회원가입
- 이메일 로그인
- 로그아웃
- 로그인 상태 변경 이벤트 발행

초기화 시 `FirebaseInitializer.Instance.Auth`를 받아오고 `auth.StateChanged` 이벤트를 구독합니다.

로그인 성공 시 `currentUser`를 갱신하고 `LoginStatusChanged` 이벤트를 발생시킵니다.

주요 메서드는 다음과 같습니다.

- `SignInAnonymousAsync()`
- `CreateUserWithEmailAsync(string email, string pw)`
- `SignInUserWithEmailAsync(string email, string pw)`
- `SignOut()`

다른 시스템은 다음 값들을 사용합니다.

```csharp
AuthManager.Instance.IsLoggedIn
AuthManager.Instance.UserId
AuthManager.Instance.CurrentUser
```

로그인 상태가 바뀌면 `LoginStatusChanged` 이벤트를 통해 `ScoreManager` 같은 다른 매니저가 반응합니다.

### ProfileManager

파일: `Assets/Scripts/ProfileManager.cs`

유저 프로필을 Realtime Database에 저장하고 불러오는 매니저입니다.

사용 경로는 다음과 같습니다.

```text
users/{userId}
```

저장 데이터는 `UserProfile` 클래스 구조를 따릅니다.

```text
nickname
email
createdAt
```

주요 메서드는 다음과 같습니다.

- `SaveProfileAsync(string nickname)`
- `LoadProfileAsync()`
- `UpdateNickNameAsync(string nickname)`

프로필 저장 시 현재 로그인된 유저의 UID를 기준으로 다음 위치에 JSON을 저장합니다.

```csharp
userRef.Child(userId).SetRawJsonValueAsync(json);
```

닉네임만 수정할 때는 다음 경로를 직접 갱신합니다.

```csharp
users/{userId}/nickname
```

### ScoreManager

파일: `Assets/Scripts/ScoreManager.cs`

게임 점수 저장 담당 매니저입니다.

초기화 시 Firebase Database 준비를 기다린 뒤 `scores` 경로를 잡습니다.

```text
scores
```

그리고 `GameEvents.GameEnded` 이벤트를 구독합니다. 게임이 끝나면 `GameManager`가 `GameEvents.RaiseGameEnded(score)`를 호출하고, `ScoreManager`가 점수를 저장합니다.

점수 저장 경로는 다음과 같습니다.

```text
scores/{userId}/history/{pushId}
```

저장 데이터는 다음 형태입니다.

```text
score
timestamp
```

`timestamp`는 Firebase 서버 시간을 사용합니다.

```csharp
ServerValue.Timestamp
```

최고 점수는 다음 경로에 저장됩니다.

```text
scores/{userId}/bestscore
```

점수 저장 흐름은 다음과 같습니다.

1. 로그인 상태인지 확인합니다.
2. `scores/{userId}/history` 아래에 `Push()`로 새 기록 노드를 만듭니다.
3. 현재 점수와 서버 timestamp를 저장합니다.
4. 현재 점수가 캐시된 최고 점수보다 높으면 `bestscore`를 갱신합니다.
5. 최고 점수 갱신 시 `LeaderboardManager.SaveToLeaderboard(score)`를 호출해 리더보드에도 반영합니다.

주요 메서드는 다음과 같습니다.

- `SaveScoreAsync(int score)`
- `LoadBestScoreAsync()`
- `LoadHistoryAsync(int limit = 10)`

### LeaderboardManager

파일: `Assets/Scripts/LeaderboardManager.cs`

전체 유저의 최고 점수 리더보드를 관리합니다.

사용 경로는 다음과 같습니다.

```text
leaderboard/{userId}
```

저장 데이터는 `LeaderboardEntry` 구조를 따릅니다.

```text
userId
nickname
score
timestamp
```

리더보드 저장 시 유저 ID를 key로 사용합니다. 따라서 같은 유저가 다시 최고 점수를 갱신하면 기존 리더보드 항목이 덮어써집니다.

```csharp
leaderboardRef.Child(userId).UpdateChildrenAsync(entryData);
```

리더보드 조회는 점수를 기준으로 정렬한 뒤 상위 N명을 가져옵니다.

```csharp
leaderboardRef.OrderByChild("score").LimitToLast(limit);
```

Firebase Realtime Database는 오름차순 기준으로 가져오기 때문에, 코드에서는 가져온 뒤 C# 리스트에서 다시 내림차순 정렬합니다.

```csharp
list.Sort((a, b) => b.score.CompareTo(a.score));
```

실시간 리더보드는 `ValueChanged` 이벤트를 사용합니다.

```csharp
listenerQuery.ValueChanged += OnValueChanged;
```

리스너가 활성화되어 있으면 Firebase DB의 리더보드 값이 바뀔 때마다 `OnLeaderboardUpdated` 이벤트를 UI에 전달합니다.

## 데이터 모델 클래스

### UserProfile

파일: `Assets/Scripts/UserProfile.cs`

유저 프로필 데이터입니다.

```text
nickname
email
createdAt
```

`JsonUtility.ToJson()`과 `JsonUtility.FromJson<UserProfile>()`으로 Firebase에 저장하거나 읽습니다.

### ScoreData

파일: `Assets/Scripts/ScoreData.cs`

점수 히스토리 데이터입니다.

```text
score
timestamp
```

timestamp를 사람이 읽을 수 있는 날짜 문자열로 바꾸는 헬퍼 메서드를 가지고 있습니다.

### LeaderboardEntry

파일: `Assets/Scripts/LeaderboardEntry.cs`

리더보드에 저장되는 유저별 최고 점수 데이터입니다.

```text
userId
nickname
score
timestamp
```

### FirebaseValue

파일: `Assets/Scripts/FirebaseValue.cs`

Firebase에서 읽은 값을 `int`로 변환하는 유틸리티입니다.

Realtime Database에서 숫자가 `long`, `double`, `int`, 문자열 등으로 들어올 수 있기 때문에 이를 안전하게 `int`로 변환합니다.

## UI와 Firebase 연결

### LoginUI

파일: `Assets/Scripts/LoginUI.cs`

로그인 UI 버튼과 `AuthManager`를 연결합니다.

- 로그인 버튼 클릭 시 `SignInUserWithEmailAsync()`
- 회원가입 버튼 클릭 시 `CreateUserWithEmailAsync()`
- 익명 로그인 버튼 클릭 시 `SignInAnonymousAsync()`
- 로그인 상태에 따라 로그인 패널 표시 여부 변경

### ProfileUI

파일: `Assets/Scripts/ProfileUI.cs`

프로필 패널을 열 때 `ProfileManager.LoadProfileAsync()`를 호출해서 닉네임을 표시합니다. 로그아웃 버튼을 누르면 `AuthManager.SignOut()`을 호출합니다.

### ProfileEditUI

파일: `Assets/Scripts/ProfileEditUI.cs`

프로필 생성 또는 닉네임 수정 UI입니다.

- 새 프로필 저장 시 `ProfileManager.SaveProfileAsync()`
- 닉네임 수정 시 `ProfileManager.UpdateNickNameAsync()`

### HistoryUI

파일: `Assets/Scripts/HistoryUI.cs`

점수 기록 화면입니다.

- 최고 점수는 `ScoreManager.LoadBestScoreAsync()`
- 점수 히스토리는 `ScoreManager.LoadHistoryAsync()`

### LeaderboardUI

파일: `Assets/Scripts/LeaderboardUI.cs`

리더보드 화면입니다.

- 수동 새로고침 시 `LeaderboardManager.LoadLeaderboardAsync()`
- 실시간 토글 ON 시 `LeaderboardManager.StartRealtimeListener()`
- 실시간 토글 OFF 시 `LeaderboardManager.StopRealtimeListener()`

## Realtime Database 구조

다른 프로젝트에 적용할 때도 같은 구조를 쓰면 현재 코드 흐름을 거의 그대로 사용할 수 있습니다.

```text
users
  {userId}
    nickname: string
    email: string
    createdAt: number

scores
  {userId}
    bestscore: number
    history
      {pushId}
        score: number
        timestamp: number

leaderboard
  {userId}
    userId: string
    nickname: string
    score: number
    timestamp: number
```

## 다른 프로젝트에 적용하는 방법

### 1. Firebase SDK 추가

Unity 프로젝트에 Firebase Unity SDK를 추가합니다.

현재 코드 기준으로 필요한 Firebase 패키지는 다음입니다.

- Firebase App
- Firebase Auth
- Firebase Database

### 2. Firebase 프로젝트 설정 파일 추가

대상 플랫폼에 맞는 Firebase 설정 파일을 Unity 프로젝트에 넣습니다.

- Android: `google-services.json`
- iOS: `GoogleService-Info.plist`

Firebase 콘솔에서 Authentication과 Realtime Database를 활성화해야 합니다.

Authentication에서는 사용할 로그인 방식을 켭니다.

- Email/Password
- Anonymous

### 3. FirebaseConfig.asset 생성

`Assets/Resources` 폴더에 `FirebaseConfig.asset`을 생성합니다.

Unity 메뉴에서 다음 항목으로 만들 수 있습니다.

```text
Create > Firebase Study > Firebase Config
```

`databaseUrl`에 Firebase Realtime Database URL을 넣습니다.

예시는 다음과 같습니다.

```text
https://your-project-default-rtdb.firebaseio.com
```

### 4. 핵심 스크립트 복사

다른 프로젝트에 최소한 다음 스크립트를 복사합니다.

```text
FirebaseInitializer.cs
FirebaseConfig.cs
AuthManager.cs
ProfileManager.cs
ScoreManager.cs
LeaderboardManager.cs
FirebaseValue.cs
UserProfile.cs
ScoreData.cs
LeaderboardEntry.cs
GameEvents.cs
TimeUtil.cs
```

UI까지 그대로 가져가려면 다음 스크립트도 함께 복사합니다.

```text
LoginUI.cs
ProfileUI.cs
ProfileEditUI.cs
HistoryUI.cs
LeaderboardUI.cs
```

### 5. 씬에 매니저 오브젝트 배치

초기 씬에 다음 매니저 컴포넌트를 가진 GameObject를 배치합니다.

```text
FirebaseInitializer
AuthManager
ProfileManager
ScoreManager
LeaderboardManager
```

각 매니저는 싱글톤이며 `DontDestroyOnLoad`를 사용하므로, 여러 씬에 중복 배치하지 않는 것이 좋습니다.

권장 초기화 순서는 다음과 같습니다.

1. FirebaseInitializer
2. AuthManager
3. ProfileManager
4. ScoreManager
5. LeaderboardManager

코드에서 비동기로 기다리는 구조가 있으므로 절대적인 실행 순서 의존은 줄어들지만, 초기 씬에 모두 존재해야 `Instance` 접근 오류를 피할 수 있습니다.

### 6. 게임 종료 시 점수 이벤트 호출

게임이 끝나는 지점에서 다음 코드를 호출합니다.

```csharp
GameEvents.RaiseGameEnded(finalScore);
```

그러면 `ScoreManager`가 자동으로 점수를 저장합니다.

### 7. 리더보드 표시

리더보드 UI가 필요하면 `LeaderboardManager.LoadLeaderboardAsync()`를 호출해 수동 로드하거나, `StartRealtimeListener()`를 호출해 실시간 업데이트를 받을 수 있습니다.

```csharp
List<LeaderboardEntry> entries = await LeaderboardManager.Instance.LoadLeaderboardAsync(10);
```

실시간 업데이트를 쓰려면 다음 이벤트를 구독합니다.

```csharp
LeaderboardManager.Instance.OnLeaderboardUpdated += OnLeaderboardUpdated;
LeaderboardManager.Instance.StartRealtimeListener(10);
```

## 적용 시 주의할 점

### Firebase 초기화 전 접근 금지

Firebase Auth나 Database를 바로 사용하지 말고 반드시 초기화를 기다려야 합니다.

```csharp
await FirebaseInitializer.Instance.WaitForInitializationAsync();
```

### 로그인되지 않은 상태 처리

프로필, 점수, 리더보드는 기본적으로 로그인된 유저 ID를 기준으로 동작합니다. 따라서 저장/조회 전에 `AuthManager.Instance.IsLoggedIn`을 확인해야 합니다.

### 프로필 생성 흐름 확인

리더보드 저장 시 닉네임은 `ProfileManager.Instance.CachedProfile.nickname`에서 가져옵니다. 따라서 리더보드를 쓰기 전에 프로필이 생성되어 있어야 합니다.

프로필이 없는 유저가 점수를 저장하는 경우를 고려하려면 기본 닉네임 처리나 프로필 자동 생성 로직을 추가하는 것이 좋습니다.

### Realtime Database 보안 규칙

개발 중에는 임시로 느슨한 규칙을 쓸 수 있지만, 실제 서비스에서는 반드시 유저별 접근 권한을 제한해야 합니다.

예시 규칙 방향은 다음과 같습니다.

```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "auth != null && auth.uid == $uid",
        ".write": "auth != null && auth.uid == $uid"
      }
    },
    "scores": {
      "$uid": {
        ".read": "auth != null && auth.uid == $uid",
        ".write": "auth != null && auth.uid == $uid"
      }
    },
    "leaderboard": {
      ".read": "auth != null",
      "$uid": {
        ".write": "auth != null && auth.uid == $uid"
      }
    }
  }
}
```

### 현재 코드에서 개선하면 좋은 부분

현재 프로젝트 코드에는 다음 개선 여지가 있습니다.

- 일부 한글 로그와 UI 문자열 인코딩이 깨져 있습니다.
- `LeaderboardManager`의 일부 문자열 리터럴이 깨져 컴파일 오류가 날 수 있습니다.
- `ScoreManager.LoadHistoryAsync(int limit)`에서 쿼리를 만들지만 실제 조회에는 limit 쿼리를 사용하지 않고 전체 history를 읽고 있습니다.
- `ProfileEditUI.OpenProfileEditPanelAsync()`에서 프로필이 없을 때 생성 패널을 보여주는 흐름이 빠져 있습니다.
- `AuthManager.SignInAnonymousAsync()`에서는 로그인 성공 후 `NotifyLoginState()` 호출이 다른 로그인 메서드와 다르게 명시되어 있지 않습니다. Auth 상태 변경 이벤트로 처리될 수 있지만, 일관성을 위해 직접 호출하는 편이 안전합니다.
- `ScoreManager.OnDestroy()`에서 `AuthManager.Instance`가 이미 사라진 경우 null 참조가 날 수 있습니다.

다른 프로젝트에 옮길 때는 위 항목을 먼저 정리한 버전을 사용하는 것이 좋습니다.

## 요약

이 시스템은 `FirebaseInitializer`를 중심으로 Firebase 준비 상태를 관리하고, `AuthManager`가 로그인 유저를 관리하며, 나머지 매니저들이 로그인 유저 ID를 기준으로 Realtime Database에 데이터를 저장하는 구조입니다.

다른 프로젝트에 적용하려면 Firebase SDK와 설정 파일을 추가하고, 핵심 매니저 스크립트를 초기 씬에 배치한 뒤, 게임 종료 시 `GameEvents.RaiseGameEnded(score)`만 호출하면 점수 저장과 리더보드 반영 흐름을 재사용할 수 있습니다.
