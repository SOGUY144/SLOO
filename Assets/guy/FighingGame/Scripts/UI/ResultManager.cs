using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class ResultManager : MonoBehaviour
{
    [Header("Custom UI Elements")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button restartButton;
    public Button mainMenuButton;

    [Header("UI Transition (Optional)")]
    public Image dissolveOverlay;
    public float dissolveDuration = 1.5f;

    [Header("Auto-Generation Settings")]
    public bool createPanelIfMissing = true;
    public string winText = "YOU WIN!";
    public string loseText = "YOU LOSE...";
    public string mainMenuSceneName = "MainMenu";

    private bool isResultActive = false;

    void Start()
    {
        if (createPanelIfMissing && (resultPanel == null || resultText == null))
            CreateResultPanel();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (dissolveOverlay != null)
            dissolveOverlay.gameObject.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartMatch);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(LoadMainMenu);

        if (RoundManager.Instance != null)
            RoundManager.Instance.OnGameEnd += ShowGameResult;
    }

    void Update()
    {
        if (isResultActive)
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
        SetResult(playerWon);
    }

    public void SetResult(bool playerWon)
    {
        if (resultText != null)
        {
            resultText.text = playerWon ? winText : loseText;
        }

        StartCoroutine(ShowResultSequence());
    }

    private IEnumerator ShowResultSequence()
    {
        // --- สั่งปิดหลอดเลือดและ UI ด้านบนทันทีที่เกมจบ ---
        if (HUDController.Instance != null)
        {
            HUDController.Instance.HideGameHUDs();
        }

        if (dissolveOverlay != null)
        {
            dissolveOverlay.gameObject.SetActive(true);

            Color c = dissolveOverlay.color;

            float elapsed = 0;

            // Fade To Black
            while (elapsed < dissolveDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;

                c.a = Mathf.Clamp01(elapsed / (dissolveDuration * 0.5f));

                dissolveOverlay.color = c;

                yield return null;
            }

            c.a = 1f;
            dissolveOverlay.color = c;

            // เปิด Result Panel
            if (resultPanel != null)
                resultPanel.SetActive(true);

            // Fade From Black
            elapsed = 0;

            while (elapsed < dissolveDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;

                c.a = Mathf.Clamp01(1f - (elapsed / (dissolveDuration * 0.5f)));

                dissolveOverlay.color = c;

                yield return null;
            }

            dissolveOverlay.gameObject.SetActive(false);
        }
        else
        {
            if (resultPanel != null)
                resultPanel.SetActive(true);
        }

        // หยุดเวลาเกม
        Time.timeScale = 0f;
        isResultActive = true;
    }

    public void RestartMatch()
    {
        Time.timeScale = 1f;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        // คืนเวลาเกม
        Time.timeScale = 1f;

        // ปลดล็อกเมาส์
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // ปิด Result Panel
        if (resultPanel != null)
            resultPanel.SetActive(false);

        // ปิด Overlay
        if (dissolveOverlay != null)
            dissolveOverlay.gameObject.SetActive(false);

        // ลบ Canvas ที่สร้าง runtime
        Canvas runtimeCanvas = null;

        if (resultPanel != null)
        {
            runtimeCanvas = resultPanel.GetComponentInParent<Canvas>();
        }

        if (runtimeCanvas != null)
        {
            Destroy(runtimeCanvas.gameObject);
        }

        // รีเซ็ตสถานะ
        isResultActive = false;

        // โหลด Main Menu
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void CreateResultPanel()
    {
        // ตรวจสอบ EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");

            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // สร้าง Canvas
        GameObject canvasObject = new GameObject("Result_Canvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        // Result Panel
        resultPanel = new GameObject("Auto_ResultPanel", typeof(RectTransform));

        resultPanel.transform.SetParent(canvas.transform, false);

        Image panelImage = resultPanel.AddComponent<Image>();

        panelImage.color = new Color(0f, 0f, 0f, 0.78f);

        RectTransform panelRect = resultPanel.GetComponent<RectTransform>();

        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Result Text
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

        // Buttons
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