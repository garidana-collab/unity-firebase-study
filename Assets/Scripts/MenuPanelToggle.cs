using UnityEngine;
using UnityEngine.UI;

public class MenuPanelToggle : MonoBehaviour
{
    [SerializeField]
    private Button openButton;

    [SerializeField]
    private GameObject targetPanel;

    private void Awake()
    {
        if (openButton != null && targetPanel != null)
        {
            openButton.onClick.AddListener(() => targetPanel.SetActive(true));
        }
    }
}
