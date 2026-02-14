using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene to Load")]
    [SerializeField] private string gameSceneName = "michael";

    [Header("UI References")]
    [SerializeField] private Button playButton;

    void Start()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
    }

    public void OnPlayClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
