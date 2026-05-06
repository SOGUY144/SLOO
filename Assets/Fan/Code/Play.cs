using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "GameScene"; // ชื่อ Scene ที่ต้องการไป
    // หรือจะใช้ Index แทนก็ได้
    [SerializeField] private int targetSceneIndex = 1;

    // เรียกใช้ด้วยชื่อ Scene
    public void LoadSceneByName()
    {
        SceneManager.LoadScene(targetSceneName);
    }

    // เรียกใช้ด้วย Index
    public void LoadSceneByIndex()
    {
        SceneManager.LoadScene(targetSceneIndex);
    }
}