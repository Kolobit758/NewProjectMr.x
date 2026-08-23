using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private GameObject pauseMenuUI; // ลาก Panel ของ Pause Menu มาใส่ที่นี่

    private bool isPaused = false;

    private void Update()
    {
        // กดปุ่ม Escape เพื่อสลับสถานะ Pause / Resume
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        Time.timeScale = 1f; // กลับมาเดินเวลาปกติ
        isPaused = false;
    }

    public void Pause()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        Time.timeScale = 0f; // หยุดเวลาในเกม
        isPaused = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // ต้องคืนเวลาก่อนโหลดฉากใหม่เสมอ
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMainMenu(string mainMenuSceneName = "MainMenu")
    {
        Time.timeScale = 1f; // คืนเวลาก่อนเปลี่ยนฉาก
        SceneManager.LoadScene(mainMenuSceneName);
    }
}