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
    public Text playerComboText;
    public Text opponentComboText;

    [Header("Custom UI Assets (Optional)")]
    public GameObject customFightUI;
    public GameObject customKOUI;
    public GameObject customTimeUpUI;
    public GameObject customYouWinUI;
    public GameObject customYouLoseUI;

    private Coroutine playerComboRoutine;
    private Coroutine opponentComboRoutine;

    [Header("Optional Round Win Dots")]
    public Image[] playerRoundDots;
    public Image[] opponentRoundDots;
    public Color roundDotOn = Color.white;
    public Color roundDotOff = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Header("Arcade Overlay")]
    public Image overlayBackground;
    public Color overlayColor = new Color(0f, 0f, 0f, 0.58f);
    public float messagePunchDuration = 0.12f;
    public float messageFadeDuration = 0.35f;
    public float defaultMessageHold = 1.15f;
    public float koMessageHold = 1.65f;
    public float resultMessageHold = 2.2f;

    [Header("Damage Bar Delay")]
    public float damageDelay = 0.65f;
    public float damageLerpSpeed = 8f;

    [Header("Debug")]
    public bool logHealthBarUpdates = false;

    [Header("UI Sounds")]
    public AudioClip fightSound;
    public AudioClip koSound;
    public AudioClip timeUpSound;
    public AudioClip youWinSound;
    public AudioClip youLoseSound;
    
    [Header("Round Sounds")]
    public AudioClip round1Sound;
    public AudioClip round2Sound;
    public AudioClip round3Sound;
    public AudioClip round4Sound;
    public AudioClip round5Sound;

    private RoundManager roundManager;
    private FightingController player;
    private OpponentAI opponent;
    private Coroutine messageRoutine;
    private AudioSource uiAudioSource;

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
        uiAudioSource = gameObject.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;

        roundManager = RoundManager.Instance;
        if (roundManager == null)
        {
            Debug.LogError("[HUD] RoundManager not found.");
            return;
        }

        player = roundManager.player;
        opponent = roundManager.opponent;

        EnsureAutoTextUI();
        SetOverlayVisible(false, 0f);
        SetPlayerHP(player != null ? player.currentHealth : 100, player != null ? player.maxHealth : 100);
        SetOpponentHP(opponent != null ? opponent.currentHealth : 100, opponent != null ? opponent.maxHealth : 100);
        SubscribeEvents();
    }

    void Update()
    {
        if (overlayBackground != null) overlayBackground.transform.SetAsLastSibling();
        if (timerText != null) timerText.transform.SetAsLastSibling();
        if (centerMessageShadow != null) centerMessageShadow.transform.SetAsLastSibling();
        if (centerMessageText != null) centerMessageText.transform.SetAsLastSibling();

        if (player != null) SetPlayerHP(player.currentHealth, player.maxHealth);
        if (opponent != null) SetOpponentHP(opponent.currentHealth, opponent.maxHealth);

        UpdateDamageFill(ref playerDamageRatio, ref playerDamageTimer, playerHealthFill, playerDamageFill);
        UpdateDamageFill(ref opponentDamageRatio, ref opponentDamageTimer, opponentHealthFill, opponentDamageFill);
    }

    public void SetPlayerHP(int current, int max)
    {
        float ratio = HealthRatio(current, max);
        SetFill(playerHealthFill, ratio, Image.OriginHorizontal.Left);

        if (ratio < playerDamageRatio) playerDamageTimer = damageDelay;
        else playerDamageRatio = ratio;

        if (logHealthBarUpdates) Debug.Log("[HUD] Player HP ratio: " + ratio);
    }

    public void SetOpponentHP(int current, int max)
    {
        float ratio = HealthRatio(current, max);
        SetFill(opponentHealthFill, ratio, Image.OriginHorizontal.Right);

        if (ratio < opponentDamageRatio) opponentDamageTimer = damageDelay;
        else opponentDamageRatio = ratio;

        if (logHealthBarUpdates) Debug.Log("[HUD] Opponent HP ratio: " + ratio);
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
        if (delayTimer > 0f) delayTimer -= Time.deltaTime;
        else damageRatio = Mathf.MoveTowards(damageRatio, targetRatio, damageLerpSpeed * Time.deltaTime);

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

        if (overlayBackground == null)
        {
            GameObject overlayObject = new GameObject("Auto_ArcadeOverlay", typeof(RectTransform));
            overlayObject.transform.SetParent(canvas.transform, false);
            overlayBackground = overlayObject.AddComponent<Image>();
            overlayBackground.raycastTarget = false; // ป้องกันไม่ให้จอดำบล็อคการคลิกเมาส์
            RectTransform rect = overlayObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
            timerText.raycastTarget = false;

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
            centerMessageShadow.fontSize = 100;
            centerMessageShadow.fontStyle = FontStyle.Bold;
            centerMessageShadow.color = new Color(0f, 0f, 0f, 0.7f);
            centerMessageShadow.alignment = TextAnchor.MiddleCenter;
            centerMessageShadow.text = "";
            centerMessageShadow.raycastTarget = false;

            RectTransform rect = shadowObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(5f, -5f);
            rect.sizeDelta = new Vector2(0f, 150f);
        }

        if (centerMessageText == null)
        {
            GameObject messageObject = new GameObject("Auto_CenterMessageText", typeof(RectTransform));
            messageObject.transform.SetParent(canvas.transform, false);
            centerMessageText = messageObject.AddComponent<Text>();
            centerMessageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            centerMessageText.fontSize = 100;
            centerMessageText.fontStyle = FontStyle.Bold;
            centerMessageText.color = Color.white;
            centerMessageText.alignment = TextAnchor.MiddleCenter;
            centerMessageText.text = "";
            centerMessageText.raycastTarget = false;

            RectTransform rect = messageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 150f);
        }

        if (playerComboText == null)
        {
            GameObject comboObj = new GameObject("Auto_PlayerComboText", typeof(RectTransform));
            comboObj.transform.SetParent(canvas.transform, false);
            playerComboText = comboObj.AddComponent<Text>();
            playerComboText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            playerComboText.fontSize = 50;
            playerComboText.fontStyle = FontStyle.Bold;
            playerComboText.color = new Color(1f, 0.4f, 0f, 1f);
            playerComboText.alignment = TextAnchor.MiddleLeft;
            playerComboText.text = "";
            playerComboText.raycastTarget = false;

            RectTransform rect = comboObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(50f, 0f);
            rect.sizeDelta = new Vector2(400f, 200f);
        }

        if (opponentComboText == null)
        {
            GameObject comboObj = new GameObject("Auto_OpponentComboText", typeof(RectTransform));
            comboObj.transform.SetParent(canvas.transform, false);
            opponentComboText = comboObj.AddComponent<Text>();
            opponentComboText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            opponentComboText.fontSize = 50;
            opponentComboText.fontStyle = FontStyle.Bold;
            opponentComboText.color = new Color(1f, 0.4f, 0f, 1f);
            opponentComboText.alignment = TextAnchor.MiddleRight;
            opponentComboText.text = "";
            opponentComboText.raycastTarget = false;

            RectTransform rect = comboObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-50f, 0f);
            rect.sizeDelta = new Vector2(400f, 200f);
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

        roundManager.OnMessageShow += message =>
        {
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(ShowMessage(message));
        };
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
        // ปิด Custom UI ทั้งหมดก่อนเริ่ม
        HideAllCustomUI();

        if (string.IsNullOrEmpty(message))
        {
            SetMessageText("");
            SetOverlayVisible(false, 0f);
            yield break;
        }

        if (uiAudioSource != null)
        {
            if (message == "FIGHT!" && fightSound != null) uiAudioSource.PlayOneShot(fightSound);
            else if (message == "K.O.!" && koSound != null) uiAudioSource.PlayOneShot(koSound);
            else if (message == "TIME UP!" && timeUpSound != null) uiAudioSource.PlayOneShot(timeUpSound);
            else if (message == "YOU WIN!" && youWinSound != null) uiAudioSource.PlayOneShot(youWinSound);
            else if (message == "YOU LOSE..." && youLoseSound != null) uiAudioSource.PlayOneShot(youLoseSound);
            else if (message == "ROUND 1" && round1Sound != null) uiAudioSource.PlayOneShot(round1Sound);
            else if (message == "ROUND 2" && round2Sound != null) uiAudioSource.PlayOneShot(round2Sound);
            else if (message == "ROUND 3" && round3Sound != null) uiAudioSource.PlayOneShot(round3Sound);
            else if (message == "ROUND 4" && round4Sound != null) uiAudioSource.PlayOneShot(round4Sound);
            else if (message == "ROUND 5" && round5Sound != null) uiAudioSource.PlayOneShot(round5Sound);
        }

        // เช็คว่ามี Custom UI สำหรับข้อความนี้ไหม
        bool hasCustomUI = ShowCustomUIIfAvailable(message);

        MessageStyle style = GetMessageStyle(message);
        SetOverlayVisible(style.showOverlay, style.overlayAlpha);

        // ถ้าไม่มี Custom UI ถึงจะโชว์ Text ปกติ
        if (!hasCustomUI)
        {
            SetMessageText(message);
            SetMessageColor(style.color);
        }
        else
        {
            SetMessageText(""); // ซ่อนข้อความปกติ
        }

        SetMessageScale(style.startScale);

        float punchTime = 0f;
        while (punchTime < messagePunchDuration)
        {
            punchTime += Time.deltaTime;
            float scale = Mathf.Lerp(style.startScale, 1f, punchTime / messagePunchDuration);
            SetMessageScale(scale);
            yield return null;
        }

        yield return new WaitForSeconds(style.holdTime);

        float fadeTime = 0f;
        while (fadeTime < messageFadeDuration)
        {
            fadeTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, fadeTime / messageFadeDuration);
            SetMessageAlpha(style.color, alpha);
            SetOverlayVisible(style.showOverlay, style.overlayAlpha * alpha);
            yield return null;
        }

        SetMessageText("");
        SetOverlayVisible(false, 0f);
        HideAllCustomUI();
    }

    private void HideAllCustomUI()
    {
        if (customFightUI != null) customFightUI.SetActive(false);
        if (customKOUI != null) customKOUI.SetActive(false);
        if (customTimeUpUI != null) customTimeUpUI.SetActive(false);
        if (customYouWinUI != null) customYouWinUI.SetActive(false);
        if (customYouLoseUI != null) customYouLoseUI.SetActive(false);
    }

    private bool ShowCustomUIIfAvailable(string message)
    {
        if (message == "FIGHT!" && customFightUI != null) { customFightUI.SetActive(true); return true; }
        if (message == "K.O.!" && customKOUI != null) { customKOUI.SetActive(true); return true; }
        if (message == "TIME UP!" && customTimeUpUI != null) { customTimeUpUI.SetActive(true); return true; }
        if (message == "YOU WIN!" && customYouWinUI != null) { customYouWinUI.SetActive(true); return true; }
        if (message == "YOU LOSE..." && customYouLoseUI != null) { customYouLoseUI.SetActive(true); return true; }
        return false;
    }

    private MessageStyle GetMessageStyle(string message)
    {
        if (message == "FIGHT!")
            return new MessageStyle(new Color(1f, 0.82f, 0.08f, 1f), 1.5f, defaultMessageHold, false, 0f);
        if (message == "K.O.!")
            return new MessageStyle(new Color(1f, 0.1f, 0.04f, 1f), 1.8f, koMessageHold, true, overlayColor.a);
        if (message == "TIME UP!")
            return new MessageStyle(new Color(1f, 0.5f, 0.02f, 1f), 1.6f, koMessageHold, true, overlayColor.a);
        if (message == "YOU WIN!")
            return new MessageStyle(new Color(0.35f, 1f, 0.45f, 1f), 1.75f, resultMessageHold, true, overlayColor.a);
        if (message == "YOU LOSE...")
            return new MessageStyle(new Color(0.72f, 0.72f, 0.72f, 1f), 1.75f, resultMessageHold, true, overlayColor.a);
        if (message.StartsWith("ROUND"))
            return new MessageStyle(new Color(0.8f, 0.95f, 1f, 1f), 1.35f, defaultMessageHold, false, 0f);

        return new MessageStyle(Color.white, 1.35f, defaultMessageHold, false, 0f);
    }

    private void SetOverlayVisible(bool visible, float alpha)
    {
        if (overlayBackground == null) return;
        overlayBackground.enabled = visible && alpha > 0f;
        overlayBackground.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, alpha);
    }

    private void SetMessageText(string value)
    {
        if (centerMessageText != null) centerMessageText.text = value;
        if (centerMessageShadow != null) centerMessageShadow.text = value;
    }

    private void SetMessageColor(Color color)
    {
        if (centerMessageText != null) centerMessageText.color = color;
        if (centerMessageShadow != null) centerMessageShadow.color = new Color(0f, 0f, 0f, 0.7f);
    }

    private void SetMessageAlpha(Color color, float alpha)
    {
        if (centerMessageText != null) centerMessageText.color = new Color(color.r, color.g, color.b, alpha);
        if (centerMessageShadow != null) centerMessageShadow.color = new Color(0f, 0f, 0f, 0.7f * alpha);
    }

    private void SetMessageScale(float scale)
    {
        if (centerMessageText != null) centerMessageText.transform.localScale = Vector3.one * scale;
        if (centerMessageShadow != null) centerMessageShadow.transform.localScale = Vector3.one * scale;
    }

    private struct MessageStyle
    {
        public Color color;
        public float startScale;
        public float holdTime;
        public bool showOverlay;
        public float overlayAlpha;

        public MessageStyle(Color color, float startScale, float holdTime, bool showOverlay, float overlayAlpha)
        {
            this.color = color;
            this.startScale = startScale;
            this.holdTime = holdTime;
            this.showOverlay = showOverlay;
            this.overlayAlpha = overlayAlpha;
        }
    }

    public void ShowCombo(bool isPlayerCombo, int comboCount)
    {
        if (comboCount < 2) return;

        Text comboText = isPlayerCombo ? playerComboText : opponentComboText;
        if (comboText == null) return;

        comboText.text = comboCount + " HITS!";
        
        if (isPlayerCombo)
        {
            if (playerComboRoutine != null) StopCoroutine(playerComboRoutine);
            playerComboRoutine = StartCoroutine(AnimateComboText(comboText));
        }
        else
        {
            if (opponentComboRoutine != null) StopCoroutine(opponentComboRoutine);
            opponentComboRoutine = StartCoroutine(AnimateComboText(comboText));
        }
    }

    private IEnumerator AnimateComboText(Text textUI)
    {
        RectTransform rect = textUI.GetComponent<RectTransform>();
        textUI.color = new Color(textUI.color.r, textUI.color.g, textUI.color.b, 1f);
        
        float punchTime = 0.1f;
        float elapsed = 0f;
        while (elapsed < punchTime)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1.5f, 1f, elapsed / punchTime);
            rect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        rect.localScale = Vector3.one;

        yield return new WaitForSeconds(1.5f);

        float fadeTime = 0.3f;
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            textUI.color = new Color(textUI.color.r, textUI.color.g, textUI.color.b, alpha);
            yield return null;
        }

        textUI.text = "";
    }
}
