using UnityEngine;

public class SimplePause : MonoBehaviour
{
    public GameObject pausePanel; // ลาก PausePanel มาใส่ในช่องนี้ที่ Inspector
    private bool isPaused = false;

    void Update()
    {
        // เมื่อกดปุ่ม Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true);      // แสดงหน้าจอเมนู
        Time.timeScale = 0f;             // หยุดเวลาในเกม (ฟิสิกส์, การเคลื่อนที่, อนิเมชั่น จะหยุดนิ่ง)
        isPaused = true;

        // ปลดล็อกเมาส์ให้กดปุ่มบนเมนูได้
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);     // ซ่อนหน้าจอเมนู
        Time.timeScale = 1f;             // ให้เวลาเดินต่อปกติ
        isPaused = false;

        // ล็อกเมาส์กลับเข้าเกม (สำหรับเกม 3D)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}