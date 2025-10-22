using UnityEngine;
using UnityEngine.SceneManagement; // Sahne y�netimi i�in

public class PauseMenuManager : MonoBehaviour
{
    [Tooltip("Duraklatma men�s� olarak kullan�lacak UI Paneli.")]
    public GameObject pauseMenuPanel;

    [Tooltip("Y�klenecek ana men� sahnesinin ad�.")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    void Start()
    {
        // Oyun ba�lad���nda panelin kapal� oldu�undan emin ol
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Pause Menu Panel atanmam��!", this);
            enabled = false; // Script'i devre d��� b�rak
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
        // �ste�e ba�l�: Fare imlecini g�r�n�r yapabilirsiniz
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;
        Debug.Log("Oyun Duraklat�ld�.");
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f; // Oyunu devam ettir
        // �ste�e ba�l�: Fare imlecini tekrar kilitleyebilirsiniz
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
        Debug.Log("Oyun Devam Ediyor.");
    }

    public void RestartGame()
    {
        Debug.Log("Oyun yeniden ba�lat�l�yor...");
        Time.timeScale = 1f; // Sahne y�klenmeden �nce zaman� normale d�nd�r
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Mevcut sahneyi yeniden y�kle
    }

    public void LoadMainMenu()
    {
        Debug.Log($"Ana men�ye ({mainMenuSceneName}) d�n�l�yor...");
        Time.timeScale = 1f; // Sahne y�klenmeden �nce zaman� normale d�nd�r
        SceneManager.LoadScene(mainMenuSceneName);
    }
}