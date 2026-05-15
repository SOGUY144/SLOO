using UnityEngine;
using UnityEngine.UI;

public class TekkenHealthBarBuilder : MonoBehaviour
{
    [Header("Target Canvas")]
    public Canvas targetCanvas;

    [Header("Player Bar")]
    public string playerLabel = "PLAYER 1";
    public string playerName = "RYU";
    public Image playerHealthFill;

    [Header("Opponent Bar")]
    public string opponentLabel = "CPU";
    public string opponentName = "CHUN-LI";
    public Image opponentHealthFill;

    [Header("Layout")]
    public float topOffset = 32f;
    public float sidePadding = 28f;
    public float centerGap = 160f;
    public float barHeight = 18f;
    public float barWidth = 640f;
    public float meterWidth = 220f;
    public float meterHeight = 8f;

    [Header("Colors")]
    public Color frameColor = new Color(0.78f, 0.86f, 0.9f, 1f);
    public Color frameShadowColor = new Color(0.08f, 0.12f, 0.12f, 0.75f);
    public Color healthColor = new Color(0.05f, 0.78f, 0.12f, 1f);
    public Color healthHighlightColor = new Color(0.55f, 1f, 0.55f, 0.65f);
    public Color emptyColor = new Color(0.18f, 0.2f, 0.22f, 0.95f);
    public Color meterColor = new Color(0.45f, 0.72f, 0.78f, 0.85f);

    [Header("Round Dots")]
    public int roundDotCount = 2;
    public Color roundDotOnColor = Color.white;
    public Color roundDotOffColor = new Color(0.15f, 0.16f, 0.16f, 1f);
    public Color roundDotFrameColor = new Color(0.78f, 0.86f, 0.9f, 1f);
    public float roundDotSize = 12f;
    public float roundDotSpacing = 18f;

    [Header("Build")]
    public bool buildOnStart = true;
    public bool clearOldGeneratedUI = true;
    public bool autoConnectToHUDController = true;
    public bool showCenterPlate = false;

    private const string generatedRootName = "TekkenHealthBars_Generated";
    private Image[] playerRoundDots;
    private Image[] opponentRoundDots;

    void Start()
    {
        // --- ส่วนสำคัญ ---
        // เช็คก่อนว่าฉากนี้คือ "ฉากต่อสู้" จริงๆ หรือไม่ (ต้องมี RoundManager)
        // ถ้าไม่มี (เช่น อยู่ในหน้า Main Menu) ก็จะไม่สร้างหลอดเลือดเด็ดขาด!
        if (RoundManager.Instance == null)
            return;

        if (buildOnStart)
            Build();
    }

    [ContextMenu("Build Tekken Health Bars")]
    public void Build()
    {
        Canvas canvas = targetCanvas != null ? targetCanvas : FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (clearOldGeneratedUI)
        {
            Transform oldRoot = canvas.transform.Find(generatedRootName);
            if (oldRoot != null)
                DestroyImmediate(oldRoot.gameObject);
        }

        GameObject root = CreateUIObject(generatedRootName, canvas.transform);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        BuildSide(root.transform, true);
        BuildSide(root.transform, false);
        playerRoundDots = BuildRoundDots(root.transform, true);
        opponentRoundDots = BuildRoundDots(root.transform, false);
        if (showCenterPlate)
            BuildInfinityIcon(root.transform);

        ConnectToHUDController();
    }

    private void BuildSide(Transform parent, bool isPlayer)
    {
        float anchorX = isPlayer ? 0f : 1f;
        float pivotX = isPlayer ? 0f : 1f;
        float x = isPlayer ? sidePadding : -sidePadding;
        string sideName = isPlayer ? "Player" : "Opponent";

        GameObject barRoot = CreateUIObject(sideName + "_BarRoot", parent);
        RectTransform barRootRect = barRoot.GetComponent<RectTransform>();
        barRootRect.anchorMin = new Vector2(anchorX, 1f);
        barRootRect.anchorMax = new Vector2(anchorX, 1f);
        barRootRect.pivot = new Vector2(pivotX, 1f);
        barRootRect.anchoredPosition = new Vector2(x, -topOffset);
        barRootRect.sizeDelta = new Vector2(barWidth, 90f);

        BuildFrame(barRoot.transform, isPlayer);
        Image fill = BuildHealthFill(barRoot.transform, isPlayer);
        BuildHighlight(fill.transform);
        BuildSmallMeter(barRoot.transform, isPlayer);
        BuildLabels(barRoot.transform, isPlayer);

        if (isPlayer) playerHealthFill = fill;
        else opponentHealthFill = fill;
    }

    private void ConnectToHUDController()
    {
        if (!autoConnectToHUDController) return;

        HUDController hud = FindObjectOfType<HUDController>();
        if (hud == null) return;

        hud.playerHealthFill = playerHealthFill;
        hud.opponentHealthFill = opponentHealthFill;
        hud.playerRoundDots = playerRoundDots;
        hud.opponentRoundDots = opponentRoundDots;
        hud.roundDotOn = roundDotOnColor;
        hud.roundDotOff = roundDotOffColor;
        hud.RefreshHealthBars();
    }

    private Image[] BuildRoundDots(Transform parent, bool isPlayer)
    {
        int count = Mathf.Max(1, roundDotCount);
        Image[] dots = new Image[count];
        float startX = isPlayer ? -64f : 64f;
        float direction = isPlayer ? -1f : 1f;

        for (int i = 0; i < count; i++)
        {
            GameObject frame = CreateImage((isPlayer ? "Player" : "Opponent") + "_RoundDotFrame_" + i, parent, roundDotFrameColor);
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.5f, 1f);
            frameRect.anchorMax = new Vector2(0.5f, 1f);
            frameRect.pivot = new Vector2(0.5f, 1f);
            frameRect.anchoredPosition = new Vector2(startX + direction * i * roundDotSpacing, -topOffset - barHeight - 22f);
            frameRect.sizeDelta = new Vector2(roundDotSize + 5f, roundDotSize + 5f);
            frame.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

            GameObject dot = CreateImage((isPlayer ? "Player" : "Opponent") + "_RoundDot_" + i, frame.transform, roundDotOffColor);
            RectTransform dotRect = dot.GetComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = Vector2.zero;
            dotRect.sizeDelta = new Vector2(roundDotSize, roundDotSize);
            dots[i] = dot.GetComponent<Image>();
        }

        return dots;
    }

    private void BuildFrame(Transform parent, bool isPlayer)
    {
        GameObject shadow = CreateImage("FrameShadow", parent, frameShadowColor);
        RectTransform shadowRect = shadow.GetComponent<RectTransform>();
        shadowRect.anchorMin = new Vector2(0f, 1f);
        shadowRect.anchorMax = new Vector2(1f, 1f);
        shadowRect.pivot = new Vector2(0.5f, 1f);
        shadowRect.anchoredPosition = new Vector2(0f, -3f);
        shadowRect.sizeDelta = new Vector2(0f, barHeight + 12f);

        GameObject frame = CreateImage("Frame", parent, frameColor);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 1f);
        frameRect.anchorMax = new Vector2(1f, 1f);
        frameRect.pivot = new Vector2(0.5f, 1f);
        frameRect.anchoredPosition = Vector2.zero;
        frameRect.sizeDelta = new Vector2(0f, barHeight + 6f);

        GameObject empty = CreateImage("Empty", parent, emptyColor);
        RectTransform emptyRect = empty.GetComponent<RectTransform>();
        emptyRect.anchorMin = new Vector2(0f, 1f);
        emptyRect.anchorMax = new Vector2(1f, 1f);
        emptyRect.pivot = new Vector2(0.5f, 1f);
        emptyRect.anchoredPosition = new Vector2(0f, -3f);
        emptyRect.sizeDelta = new Vector2(-8f, barHeight);
    }

    private Image BuildHealthFill(Transform parent, bool isPlayer)
    {
        GameObject fillObject = CreateImage("HealthFill", parent, healthColor);
        Image fill = fillObject.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = isPlayer ? (int)Image.OriginHorizontal.Left : (int)Image.OriginHorizontal.Right;
        fill.fillAmount = 1f;

        RectTransform rect = fillObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -3f);
        rect.sizeDelta = new Vector2(-8f, barHeight);
        return fill;
    }

    private void BuildHighlight(Transform parent)
    {
        GameObject highlight = CreateImage("HealthHighlight", parent, healthHighlightColor);
        RectTransform rect = highlight.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.55f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(4f, 0f);
        rect.offsetMax = new Vector2(-4f, -2f);
    }

    private void BuildSmallMeter(Transform parent, bool isPlayer)
    {
        GameObject meterFrame = CreateImage("SmallMeterFrame", parent, frameColor);
        RectTransform frameRect = meterFrame.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(isPlayer ? 1f : 0f, 1f);
        frameRect.anchorMax = new Vector2(isPlayer ? 1f : 0f, 1f);
        frameRect.pivot = new Vector2(isPlayer ? 1f : 0f, 1f);
        frameRect.anchoredPosition = new Vector2(isPlayer ? -2f : 2f, -(barHeight + 13f));
        frameRect.sizeDelta = new Vector2(meterWidth, meterHeight + 4f);

        GameObject meter = CreateImage("SmallMeterFill", parent, meterColor);
        RectTransform meterRect = meter.GetComponent<RectTransform>();
        meterRect.anchorMin = frameRect.anchorMin;
        meterRect.anchorMax = frameRect.anchorMax;
        meterRect.pivot = frameRect.pivot;
        meterRect.anchoredPosition = new Vector2(isPlayer ? -4f : 4f, -(barHeight + 15f));
        meterRect.sizeDelta = new Vector2(meterWidth - 8f, meterHeight);
    }

    private void BuildLabels(Transform parent, bool isPlayer)
    {
        string label = isPlayer ? playerLabel : opponentLabel;
        string characterName = isPlayer ? playerName : opponentName;
        TextAnchor alignment = isPlayer ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;

        GameObject tagObject = CreateText("PlayerLabel", parent, label, 16, FontStyle.Bold, alignment);
        RectTransform tagRect = tagObject.GetComponent<RectTransform>();
        tagRect.anchorMin = new Vector2(0f, 1f);
        tagRect.anchorMax = new Vector2(1f, 1f);
        tagRect.pivot = new Vector2(0.5f, 1f);
        tagRect.anchoredPosition = new Vector2(isPlayer ? 12f : -12f, -2f);
        tagRect.sizeDelta = new Vector2(-24f, 22f);

        GameObject nameObject = CreateText("CharacterName", parent, characterName, 24, FontStyle.Bold, alignment);
        RectTransform nameRect = nameObject.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(isPlayer ? 0f : 0f, -(barHeight + 28f));
        nameRect.sizeDelta = new Vector2(0f, 34f);
    }

    private void BuildInfinityIcon(Transform parent)
    {
        GameObject center = CreateImage("InfinityBack", parent, new Color(0.8f, 0.86f, 0.86f, 0.95f));
        RectTransform centerRect = center.GetComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 1f);
        centerRect.anchorMax = new Vector2(0.5f, 1f);
        centerRect.pivot = new Vector2(0.5f, 1f);
        centerRect.anchoredPosition = new Vector2(0f, -topOffset + 8f);
        centerRect.sizeDelta = new Vector2(82f, 58f);

        GameObject textObject = CreateText("InfinityText", center.transform, "∞", 54, FontStyle.Bold, TextAnchor.MiddleCenter);
        Text text = textObject.GetComponent<Text>();
        text.color = new Color(0.35f, 0.39f, 0.39f, 1f);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject go = CreateUIObject(objectName, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    private GameObject CreateText(string objectName, Transform parent, string value, int size, FontStyle style, TextAnchor alignment)
    {
        GameObject go = CreateUIObject(objectName, parent);
        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = value;
        return go;
    }
}
