using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultManager : MonoBehaviour
{
    [Header("Custom UI Elements")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("Auto Panel")]
    public bool createPanelIfMissing = true;
    public string winText = "YOU WIN!";
    public string loseText = "YOU LOSE...";
    public string mainMenuSceneName = "MainMenu";

    void Start()
    {
        if (createPanelIfMissing && (resultPanel == null || resultText == null))
            CreateResultPanel();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        // เชื่อมคำสั่งให้ปุ่มอัตโนมัติ (ถ้ามีการลากปุ่มมาใส่ใน Inspector)
        if (restartButton != null)
            restartButton.onClick.AddListener(RestartMatch);
            
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(LoadMainMenu);

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnGameEnd += ShowGameResult;
    }

    void Update()
    {
        // บังคับปลดล็อคเมาส์ตลอดเวลาที่หน้าต่างจบเกมเปิดอยู่ (ป้องกันสคริปต์อื่น เช่น ตัวละคร ล็อคเมาส์กลับ)
        if (resultPanel != null && resultPanel.activeSelf)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void OnDestroy()
    {
        if (RoundManager.Instance != null)
            RoundManager.Instance.OnGameEnd -= ShowGameResult;
    }

    private void ShowGameResult(bool playerWon)
    {
        SetResult(playerWon ? winText : loseText);
    }

    public void SetResult(string result)
    {
        if (resultText != null)
            resultText.text = result;

        if (resultPanel != null)
            resultPanel.SetActive(true);

        Time.timeScale = 0f;

        // ปลดล็อคเมาส์เพื่อให้ผู้เล่นสามารถคลิกปุ่มได้
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void RestartMatch()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void CreateResultPanel()
    {
        // ตรวจสอบว่ามี EventSystem หรือไม่ (ถ้าไม่มีจะคลิกปุ่มไม่ได้)
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // --- สร้าง Canvas ใหม่แยกเฉพาะสำหรับ Result Panel เพื่อป้องกันบั๊กคลิกไม่ได้ ---
        GameObject canvasObject = new GameObject("Result_Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // ให้อยู่บนสุดเสมอ จะได้ไม่โดน UI อื่นบัง

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        resultPanel = new GameObject("Auto_ResultPanel", typeof(RectTransform));
        resultPanel.transform.SetParent(canvas.transform, false);
        Image panelImage = resultPanel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.78f);

        RectTransform panelRect = resultPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("ResultText", typeof(RectTransform));
        textObject.transform.SetParent(resultPanel.transform, false);
        resultText = textObject.AddComponent<TextMeshProUGUI>();
        resultText.fontSize = 96;
        resultText.fontStyle = FontStyles.Bold;
        resultText.color = Color.white;
        resultText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0.5f);
        textRect.anchorMax = new Vector2(1f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 80f);
        textRect.sizeDelta = new Vector2(0f, 140f);

        CreateButton("RestartButton", "RESTART", new Vector2(-150f, -80f), RestartMatch);
        CreateButton("MainMenuButton", "MAIN MENU", new Vector2(150f, -80f), LoadMainMenu);
    }

    private void CreateButton(string objectName, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform));
        buttonObject.transform.SetParent(resultPanel.transform, false);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(240f, 64f);

        GameObject labelObject = new GameObject("Text", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        TextMeshProUGUI buttonText = labelObject.AddComponent<TextMeshProUGUI>();
        buttonText.fontSize = 28;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.text = label;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }
}
