using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    [Header("Health Bars - Drag Your Own UI Here")]
    public Image playerHealthFill;
    public Image opponentHealthFill;
    public Image playerDamageFill;
    public Image opponentDamageFill;

    [Header("Optional Text")]
    public Text timerText;
    public Text centerMessageText;
    public Text centerMessageShadow;

    [Header("Optional Round Win Dots")]
    public Image[] playerRoundDots;
    public Image[] opponentRoundDots;
    public Color roundDotOn = Color.white;
    public Color roundDotOff = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("Damage Bar Delay")]
    public float damageDelay = 0.65f;
    public float damageLerpSpeed = 8f;

    [Header("Debug")]
    public bool logHealthBarUpdates = false;

    private RoundManager roundManager;
    private FightingController player;
    private OpponentAI opponent;

    private float playerDamageRatio = 1f;
    private float opponentDamageRatio = 1f;
    private float playerDamageTimer;
    private float opponentDamageTimer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        roundManager = RoundManager.Instance;
        if (roundManager == null)
        {
            Debug.LogError("[HUD] RoundManager not found.");
            return;
        }

        player = roundManager.player;
        opponent = roundManager.opponent;

        EnsureAutoTextUI();
        SetPlayerHP(player != null ? player.currentHealth : 100, player != null ? player.maxHealth : 100);
        SetOpponentHP(opponent != null ? opponent.currentHealth : 100, opponent != null ? opponent.maxHealth : 100);
        SubscribeEvents();
    }

    void Update()
    {
        if (player != null) SetPlayerHP(player.currentHealth, player.maxHealth);
        if (opponent != null) SetOpponentHP(opponent.currentHealth, opponent.maxHealth);

        UpdateDamageFill(ref playerDamageRatio, ref playerDamageTimer, playerHealthFill, playerDamageFill);
        UpdateDamageFill(ref opponentDamageRatio, ref opponentDamageTimer, opponentHealthFill, opponentDamageFill);
    }

    public void SetPlayerHP(int current, int max)
    {
        float ratio = HealthRatio(current, max);
        SetFill(playerHealthFill, ratio, Image.OriginHorizontal.Left);

        if (ratio < playerDamageRatio)
            playerDamageTimer = damageDelay;
        else
            playerDamageRatio = ratio;

        if (logHealthBarUpdates)
            Debug.Log("[HUD] Player HP ratio: " + ratio);
    }

    public void SetOpponentHP(int current, int max)
    {
        float ratio = HealthRatio(current, max);
        SetFill(opponentHealthFill, ratio, Image.OriginHorizontal.Right);

        if (ratio < opponentDamageRatio)
            opponentDamageTimer = damageDelay;
        else
            opponentDamageRatio = ratio;

        if (logHealthBarUpdates)
            Debug.Log("[HUD] Opponent HP ratio: " + ratio);
    }

    public void RefreshHealthBars()
    {
        if (player != null) SetPlayerHP(player.currentHealth, player.maxHealth);
        if (opponent != null) SetOpponentHP(opponent.currentHealth, opponent.maxHealth);
    }

    private void UpdateDamageFill(ref float damageRatio, ref float delayTimer, Image healthFill, Image damageFill)
    {
        if (damageFill == null || healthFill == null) return;

        float targetRatio = healthFill.fillAmount;
        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
        }
        else
        {
            damageRatio = Mathf.MoveTowards(damageRatio, targetRatio, damageLerpSpeed * Time.deltaTime);
        }

        SetFill(damageFill, damageRatio, damageFill.fillOrigin == (int)Image.OriginHorizontal.Right ? Image.OriginHorizontal.Right : Image.OriginHorizontal.Left);
    }

    private float HealthRatio(int current, int max)
    {
        if (max <= 0) return 0f;
        return Mathf.Clamp01((float)current / max);
    }

    private void SetFill(Image image, float ratio, Image.OriginHorizontal origin)
    {
        if (image == null) return;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)origin;
        image.fillAmount = ratio;

        RectTransform rect = image.rectTransform;
        if (origin == Image.OriginHorizontal.Left)
        {
            rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(ratio, rect.anchorMax.y);
        }
        else
        {
            rect.anchorMin = new Vector2(1f - ratio, rect.anchorMin.y);
            rect.anchorMax = new Vector2(1f, rect.anchorMax.y);
        }
    }

    private void EnsureAutoTextUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("HUD_Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (timerText == null)
        {
            GameObject timerObject = new GameObject("Auto_TimerText", typeof(RectTransform));
            timerObject.transform.SetParent(canvas.transform, false);
            timerText = timerObject.AddComponent<Text>();
            timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timerText.fontSize = 46;
            timerText.fontStyle = FontStyle.Bold;
            timerText.color = Color.white;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.text = "60";

            RectTransform rect = timerObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -18f);
            rect.sizeDelta = new Vector2(130f, 70f);
        }

        if (centerMessageShadow == null)
        {
            GameObject shadowObject = new GameObject("Auto_CenterMessageShadow", typeof(RectTransform));
            shadowObject.transform.SetParent(canvas.transform, false);
            centerMessageShadow = shadowObject.AddComponent<Text>();
            centerMessageShadow.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            centerMessageShadow.fontSize = 90;
            centerMessageShadow.fontStyle = FontStyle.Bold;
            centerMessageShadow.color = new Color(0f, 0f, 0f, 0.6f);
            centerMessageShadow.alignment = TextAnchor.MiddleCenter;
            centerMessageShadow.text = "";

            RectTransform rect = shadowObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(4f, -4f);
            rect.sizeDelta = new Vector2(0f, 130f);
        }

        if (centerMessageText == null)
        {
            GameObject messageObject = new GameObject("Auto_CenterMessageText", typeof(RectTransform));
            messageObject.transform.SetParent(canvas.transform, false);
            centerMessageText = messageObject.AddComponent<Text>();
            centerMessageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            centerMessageText.fontSize = 90;
            centerMessageText.fontStyle = FontStyle.Bold;
            centerMessageText.color = Color.white;
            centerMessageText.alignment = TextAnchor.MiddleCenter;
            centerMessageText.text = "";

            RectTransform rect = messageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 130f);
        }
    }

    private void SubscribeEvents()
    {
        roundManager.OnTimerUpdate += time =>
        {
            if (timerText == null) return;
            timerText.text = Mathf.CeilToInt(time).ToString("D2");
        };

        roundManager.OnRoundWinsUpdate += (playerWins, opponentWins) =>
        {
            UpdateRoundDots(playerRoundDots, playerWins);
            UpdateRoundDots(opponentRoundDots, opponentWins);
        };

        roundManager.OnMessageShow += message => StartCoroutine(ShowMessage(message));
        roundManager.OnRoundStart += ResetDamageBars;
    }

    private void UpdateRoundDots(Image[] dots, int wins)
    {
        if (dots == null) return;

        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null) dots[i].color = i < wins ? roundDotOn : roundDotOff;
        }
    }

    private void ResetDamageBars()
    {
        playerDamageRatio = 1f;
        opponentDamageRatio = 1f;
        playerDamageTimer = 0f;
        opponentDamageTimer = 0f;

        SetFill(playerHealthFill, 1f, Image.OriginHorizontal.Left);
        SetFill(opponentHealthFill, 1f, Image.OriginHorizontal.Right);
        SetFill(playerDamageFill, 1f, Image.OriginHorizontal.Left);
        SetFill(opponentDamageFill, 1f, Image.OriginHorizontal.Right);
    }

    private IEnumerator ShowMessage(string message)
    {
        StopCoroutine("ShowMessage");

        Color color = message == "FIGHT!" ? new Color(1f, 0.85f, 0.15f, 1f) :
                      message == "K.O.!" ? new Color(1f, 0.12f, 0.08f, 1f) :
                      message == "TIME UP!" ? new Color(1f, 0.55f, 0.05f, 1f) :
                      message == "YOU WIN!" ? new Color(0.35f, 1f, 0.45f, 1f) :
                      message == "YOU LOSE..." ? new Color(0.75f, 0.75f, 0.75f, 1f) : Color.white;

        if (centerMessageText != null)
        {
            centerMessageText.text = message;
            centerMessageText.color = color;
            centerMessageText.transform.localScale = Vector3.one * 1.35f;
        }

        if (centerMessageShadow != null)
        {
            centerMessageShadow.text = message;
            centerMessageShadow.color = new Color(0f, 0f, 0f, 0.6f);
            centerMessageShadow.transform.localScale = Vector3.one * 1.35f;
        }

        if (string.IsNullOrEmpty(message)) yield break;

        float punchTime = 0f;
        while (punchTime < 0.12f)
        {
            punchTime += Time.deltaTime;
            float scale = Mathf.Lerp(1.35f, 1f, punchTime / 0.12f);
            if (centerMessageText != null) centerMessageText.transform.localScale = Vector3.one * scale;
            if (centerMessageShadow != null) centerMessageShadow.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        yield return new WaitForSeconds(1.4f);

        if (centerMessageText != null) centerMessageText.text = "";
        if (centerMessageShadow != null) centerMessageShadow.text = "";
    }
}
