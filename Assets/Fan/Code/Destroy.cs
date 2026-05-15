using UnityEngine;
using UnityEngine.SceneManagement;

public class DestroyOnMainMenu : MonoBehaviour
{
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Mainmenu")
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}