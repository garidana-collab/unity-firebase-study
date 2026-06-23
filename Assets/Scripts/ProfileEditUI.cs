using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileEditUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject profileEditPanel;

    [SerializeField]
    private GameObject createProfilePanel;

    [SerializeField]
    private GameObject editProfilePanel;

    [Header("Create Profile")]
    [SerializeField]
    private TMP_InputField createNicknameInput;

    [SerializeField]
    private Button createButton;

    [SerializeField]
    private TextMeshProUGUI createErrorText;

    [Header("Edit Profile")]
    [SerializeField]
    private TextMeshProUGUI currentNicknameText;

    [SerializeField]
    private TMP_InputField editNicknameInput;

    [SerializeField]
    private Button updateButton;

    [SerializeField]
    private Button closeEditButton;

    [SerializeField]
    private TextMeshProUGUI editErrorText;

    [Header("References")]
    [SerializeField]
    private ProfileUI profileUI;

    private void Start()
    {
        createButton.onClick.AddListener(() => OnCreateButtonClicked().Forget());
        updateButton.onClick.AddListener(() => OnUpdateButtonClicked().Forget());
        closeEditButton.onClick.AddListener(OnCloseEditButtonClicked);

        profileEditPanel.SetActive(false);
    }

    public async UniTaskVoid OpenProfileEditPanelAsync()
    {
        profileEditPanel.SetActive(true);
        var (profile, _) = await ProfileManager.Instance.LoadProfileAsync();

        if (profile != null)
        {
            ShowEditProfile(profile);
        }
        
    }

    private void ShowCreateProfile()
    {
        createProfilePanel.SetActive(true);
        editProfilePanel.SetActive(false);
        createNicknameInput.text = "";
        createErrorText.text = "";
    }

    private void ShowEditProfile(UserProfile profile)
    {
        createProfilePanel.SetActive(false);
        editProfilePanel.SetActive(true);

        currentNicknameText.text = $"현재 닉네임: {profile.nickname}";
        editNicknameInput.text = profile.nickname;
        editErrorText.text = "";
    }

    private async UniTaskVoid OnCreateButtonClicked()
    {
        string nickname = createNicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            createErrorText.text = "닉네임을 입력하세요";
            createErrorText.color = Color.red;
            return;
        }

        createButton.interactable = false;

        var (success, error) = await ProfileManager.Instance.SaveProfileAsync(nickname);

        if (success)
        {
            createErrorText.text = "프로필 생성 완료!";
            createErrorText.color = Color.green;

            await UniTask.Delay(1000, cancellationToken : this.GetCancellationTokenOnDestroy());
            profileEditPanel.SetActive(false);
            profileUI.UpdateProfileUIAsync().Forget();
        }
        else
        {
            createErrorText.text = error;;
            createErrorText.color = Color.red;
        }

        createButton.interactable = true;
    }

    private async UniTaskVoid OnUpdateButtonClicked()
    {
        string nickname = editNicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            editErrorText.text = "닉네임을 입력하세요";
            editErrorText.color = Color.red;
            return;
        }

        updateButton.interactable = false;

        var (success, error) = await ProfileManager.Instance.UpdateNickNameAsync(nickname);

        if (success)
        {
            editErrorText.text = "수정 완료!";
            editErrorText.color = Color.green;
            currentNicknameText.text = $"현재 닉네임: {nickname}";

            await UniTask.Delay(1000, cancellationToken : this.GetCancellationTokenOnDestroy());
            profileEditPanel.SetActive(false);
            profileUI.UpdateProfileUIAsync().Forget();
        }
        else
        {
            createErrorText.text = error;;
            createErrorText.color = Color.red;
        }

        updateButton.interactable = true;
    }

    private void OnCloseEditButtonClicked()
    {
        profileEditPanel.SetActive(false);
    }
}
