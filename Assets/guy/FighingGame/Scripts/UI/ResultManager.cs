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
    public string dissolvePropertyName = "_DissolveAmount";
    public float startValue = 0f;
    public float endValue = 1f;

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
        if (dissolveOverlay != null)
        {
            dissolveOverlay.gameObject.SetActive(true);
            Color c = dissolveOverlay.color;

            // --- 1. ค่อยๆ มืดลง (Fade TO Black) ---
            float elapsed = 0;
            while (elapsed < dissolveDuration * 0.5f) // ใช้เวลาครึ่งแรก
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(elapsed / (dissolveDuration * 0.5f));
                dissolveOverlay.color = c;
                yield return null;
            }
            c.a = 1f;
            dissolveOverlay.color = c;

            // --- 2. จังหวะที่จอมืดสนิท ให้เปิดหน้าต่างสรุปผลขึ้นมา ---
            if (resultPanel != null)
                resultPanel.SetActive(true);

            // --- 3. ค่อยๆ สว่างกลับมา (Fade FROM Black) ---
            elapsed = 0;
            while (elapsed < dissolveDuration * 0.5f) // ใช้เวลาครึ่งหลัง
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(1f - (elapsed / (dissolveDuration * 0.5f)));
                dissolveOverlay.color = c;
                yield return null;
            }
            
            // ปิด Overlay ไปเลยเมื่อสว่างแล้ว
            dissolveOverlay.gameObject.SetActive(false);
        }
        else
        {
            // ถ้าไม่มี Overlay ก็แค่เปิด Panel เลย
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