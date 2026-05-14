using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum HitType
{
    Normal,
    Counter,
    Launch,
    GuardBreak,
    RageArt
}

public class HitPopupManager : MonoBehaviour
{
    public static HitPopupManager Instance;

    [Header("Settings")]
    public int poolSize = 10;
    public float floatSpeed = 1.0f;
    public float duration = 0.7f;
    public Vector3 baseScale = new Vector3(0.015f, 0.015f, 0.015f);

    private List<TextMeshProUGUI> textPool = new List<TextMeshProUGUI>();
    private Camera mainCam;
    private TMP_FontAsset defaultFont;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        mainCam = Camera.main;
        
        // พยายามหา Font TMP ในโปรเจคมาใช้
        defaultFont = TMP_Settings.defaultFontAsset;
        
        SetupSystem();
    }

    private void SetupSystem()
    {
        for (int i = 0; i < poolSize; i++)
        {
            // 1. สร้าง Canvas แยกชิ้นต่อ 1 ข้อความ (World Space)
            GameObject canvasObj = new GameObject("HitCanvas_" + i);
            canvasObj.transform.SetParent(transform);
            
            Canvas c = canvasObj.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.sortingOrder = 999; // ให้อยู่หน้าสุดเสมอ
            
            // 2. สร้าง Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(canvasObj.transform);
            
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) tmp.font = defaultFont;
            
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = Color.black;
            tmp.raycastTarget = false;

            // ตั้งค่า RectTransform ให้เต็ม Canvas
            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 100);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            canvasObj.transform.localScale = Vector3.zero;
            canvasObj.SetActive(false);
            textPool.Add(tmp);
        }
    }

    public void ShowHit(Vector3 worldPos, HitType type)
    {
        TextMeshProUGUI text = GetPooledText();
        if (text == null) return;

        GameObject canvasObj = text.transform.parent.gameObject;
        
        // ตั้งค่าข้อความและสีตามประเภท
        SetupTextByType(text, type);

        canvasObj.SetActive(true);
        canvasObj.transform.position = worldPos + Vector3.up * 1.5f + Random.insideUnitSphere * 0.3f;
        canvasObj.transform.localScale = Vector3.zero;

        StartCoroutine(AnimateText(canvasObj, text));
    }

    private TextMeshProUGUI GetPooledText()
    {
        for (int i = 0; i < textPool.Count; i++)
        {
            if (textPool[i] != null && !textPool[i].transform.parent.gameObject.activeInHierarchy)
            {
                return textPool[i];
            }
        }
        return null;
    }

    private void SetupTextByType(TextMeshProUGUI tmp, HitType type)
    {
        switch (type)
        {
            case HitType.Normal:
                tmp.text = "HIT";
                tmp.color = Color.white;
                tmp.fontSize = 30;
                break;
            case HitType.Counter:
                tmp.text = "COUNTER!";
                tmp.color = Color.yellow;
                tmp.fontSize = 45;
                break;
            case HitType.Launch:
                tmp.text = "LAUNCH!";
                tmp.color = new Color(0.4f, 0.7f, 1f);
                tmp.fontSize = 45;
                break;
            case HitType.GuardBreak:
                tmp.text = "GUARD BREAK!";
                tmp.color = Color.red;
                tmp.fontSize = 40;
                break;
            case HitType.RageArt:
                tmp.text = "RAGE ART!";
                tmp.color = new Color(1f, 0.2f, 0.2f);
                tmp.fontSize = 60;
                break;
        }
    }

    private IEnumerator AnimateText(GameObject obj, TextMeshProUGUI tmp)
    {
        float elapsed = 0;
        Vector3 startPos = obj.transform.position;
        Color startCol = tmp.color;

        // Scale Punch
        float punchTime = 0.1f;
        while (elapsed < punchTime)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(0, 1.2f, elapsed / punchTime);
            obj.transform.localScale = baseScale * s;
            
            Billboard(obj.transform);
            yield return null;
        }

        elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            obj.transform.position = startPos + Vector3.up * (floatSpeed * t);
            tmp.color = new Color(startCol.r, startCol.g, startCol.b, 1 - t);

            if (elapsed < 0.1f)
            {
                float s = Mathf.Lerp(1.2f, 1.0f, elapsed / 0.1f);
                obj.transform.localScale = baseScale * s;
            }

            Billboard(obj.transform);
            yield return null;
        }

        obj.SetActive(false);
    }

    private void Billboard(Transform t)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam != null)
        {
            t.LookAt(t.position + mainCam.transform.forward);
        }
    }
}
