using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene to Load")]
    [SerializeField] private string gameSceneName = "Jean";

    [Header("UI References")]
    [SerializeField] private Button playButton;

    void Start()
    {
        // Force cursor visible and unlocked for menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
    }

    public void OnPlayClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
