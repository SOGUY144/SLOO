using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HardMode : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "GameScene";
    [SerializeField] private int targetSceneIndex = 1;
    [SerializeField] private bool useSceneName = true;

    [Header("Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    void Start()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        if (yesButton != null)
            yesButton.onClick.AddListener(OnYes);

        if (noButton != null)
            noButton.onClick.AddListener(OnNo);
    }

    public void ShowConfirmPanel()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(true);
    }

    private void OnYes()
    {
        // ค้าง panel ไว้จนกว่าจะเปลี่ยน scene
        if (useSceneName)
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.GoToScene(targetSceneName);
            else
                SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneIndex);
        }
    }

    private void OnNo()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }
}