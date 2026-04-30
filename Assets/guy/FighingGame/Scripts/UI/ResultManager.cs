using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultManager : MonoBehaviour
{
    public GameObject resultPanel;
    public Text resultText;

    public FightingController[] fightingController;
    public OpponentAI[] opponentAI;

    void Update()
{
    // เช็คว่า Player ตายหรือยัง
    foreach (FightingController fightingController in fightingController)
    {
        if (fightingController.gameObject.activeSelf && fightingController.currentHealth <= 0)
        {
            SetResult("You Lose!");
            return;
        }
    }

    // เช็คว่า Opponent ตายหรือยัง
    foreach (OpponentAI opponentAI in opponentAI)
    {
        if (opponentAI.gameObject.activeSelf && opponentAI.currentHealth <= 0)
        {
            SetResult("You Win!");
            return;
        }
    }
}


    // ฟังก์ชันสำหรับตั้งค่าผลลัพธ์ (ชนะ/แพ้)
    public void SetResult(string result)
    {
        resultText.text = result;
        resultPanel.SetActive(true);
        Time.timeScale = 0f; // หยุดเวลา
    }

    // ฟังก์ชันเมื่อกดปุ่มกลับสู่เมนูหลัก
    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // ให้เวลาเดินต่อก่อนโหลดฉาก
        SceneManager.LoadScene("MainMenu");
    }
}
