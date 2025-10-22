using UnityEngine;
using UnityEngine.SceneManagement; // Sahne yönetimi için

public class PauseMenuManager : MonoBehaviour
{
    [Tooltip("Duraklatma menüsü olarak kullanýlacak UI Paneli.")]
    public GameObject pauseMenuPanel;

    [Tooltip("Yüklenecek ana menü sahnesinin adý.")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    void Start()
    {
        // Oyun baþladýðýnda panelin kapalý olduðundan emin ol
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Pause Menu Panel atanmamýþ!", this);
            enabled = false; // Script'i devre dýþý býrak
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f; // Oyunu duraklat
        // Ýsteðe baðlý: Fare imlecini görünür yapabilirsiniz
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;
        Debug.Log("Oyun Duraklatýldý.");
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f; // Oyunu devam ettir
        // Ýsteðe baðlý: Fare imlecini tekrar kilitleyebilirsiniz
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
        Debug.Log("Oyun Devam Ediyor.");
    }

    public void RestartGame()
    {
        Debug.Log("Oyun yeniden baþlatýlýyor...");
        Time.timeScale = 1f; // Sahne yüklenmeden önce zamaný normale döndür
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Mevcut sahneyi yeniden yükle
    }

    public void LoadMainMenu()
    {
        Debug.Log($"Ana menüye ({mainMenuSceneName}) dönülüyor...");
        Time.timeScale = 1f; // Sahne yüklenmeden önce zamaný normale döndür
        SceneManager.LoadScene(mainMenuSceneName);
    }
}