using UnityEngine;

public class QuitButton : MonoBehaviour
{
    public void OnClickQuit()
    {
        // ถ้ารันใน Unity Editor จะออกจาก Play Mode
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // ถ้า Build แล้วจะปิดเกมเลย
        Application.Quit();
#endif
    }
}