using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using System.Linq;

public class VoiceSkillController : MonoBehaviour
{
    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, System.Action> actions = new Dictionary<string, System.Action>();

    [Header("Voice Magic Settings")]
    public float skillRadius = 15f;    // รัศมีสกิล
    public float skillCooldown = 2f;   // คูลดาวน์เวทย์มนตร์
    private float lastSkillTime = 0f;

    [Header("Skill Damages")]
    [Tooltip("ดาเมจของสกิล FIRE")]
    public int fireDamage = 15;
    [Tooltip("ดาเมจของสกิล PUSH")]
    public int pushDamage = 10;
    [Tooltip("ดาเมจของสกิล BOOM")]
    public int boomDamage = 25;

    [Header("Visual Effects")]
    public ParticleSystem fireEffect;
    public ParticleSystem pushEffect;
    public ParticleSystem boomEffect;

    [Header("UI System (ไมโครโฟน)")]
    public GameObject micUIPanel;           
    public UnityEngine.UI.Text micStatusText; 
    
    [Header("Microphone Settings")]
    [Tooltip("ความไวของหลอดเสียง (ยิ่งเยอะ ยิ่งขยับง่าย)")]
    public float micSensitivity = 30f;

    // ตั้งค่าหลอดวัดเสียงแบบจุดๆ (Segment)
    public int segmentCount = 15;
    private UnityEngine.UI.Image[] volumeSegments;

    private bool isListening = false;
    private AudioClip micClip; // เอาไว้อัดเสียงชั่วคราวเพื่อวัดระดับความดัง

    void Start()
    {
        // สร้าง UI อัตโนมัติถ้ายังไม่ได้ลากใส่ช่อง
        if (micUIPanel == null || micStatusText == null)
        {
            CreateUIAutomatically();
        }

        // เช็คว่ามีไมค์เสียบอยู่ไหม
        if (Microphone.devices.Length > 0)
        {
            Debug.Log("🎙️ --- ตรวจพบไมโครโฟนในระบบทั้งหมด " + Microphone.devices.Length + " ตัว ---");
            Debug.Log("✅ กำลังใช้งานไมค์ Default ของระบบ (ตัวเดียวกับที่ใช้ร่ายเวทย์)");
        }
        else
        {
            Debug.LogError("❌ ไม่พบไมโครโฟน! รบกวนเช็คสายแจ็ค หรือตั้งค่าใน Windows ครับ");
        }

        // กำหนดคำศัพท์ที่จะใช้ร่ายเวทย์
        actions.Add("fire", FireSkill);
        actions.Add("push", PushSkill);
        actions.Add("boom", BoomSkill);

        // สร้าง Recognizer และผูกกับคำที่เราตั้งไว้
        keywordRecognizer = new KeywordRecognizer(actions.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += RecognizedSpeech;
    }

    public void StartListening()
    {
        if (keywordRecognizer != null && !keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Start();
            isListening = true;
            
            // เริ่มวัดระดับเสียงด้วยไมค์ Default (null)
            micClip = Microphone.Start(null, true, 10, 44100);
            
            // เปิด UI ไมโครโฟน
            if (micUIPanel != null) micUIPanel.SetActive(true);
            if (micStatusText != null) micStatusText.text = "🎤 พร้อมร่ายเวทย์!";
            
            Debug.Log("🎤 Voice Magic Started! You can now cast spells.");
        }
    }

    public void StopListening()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();
            isListening = false;
            
            // หยุดวัดระดับเสียง
            Microphone.End(null);
            
            // ปิด UI ไมโครโฟน
            if (micUIPanel != null) micUIPanel.SetActive(false);
            
            Debug.Log("🎤 Voice Magic Stopped! (Dropped book)");
        }
    }

    private void RecognizedSpeech(PhraseRecognizedEventArgs speech)
    {
        if (!isListening) return;

        Debug.Log("🗣️ You casted: " + speech.text);
        
        // เช็ค Cooldown
        if (Time.time - lastSkillTime < skillCooldown)
        {
            if (micStatusText != null) micStatusText.text = "COOLDOWN...";
            Debug.Log("Skill is on cooldown!");
            return;
        }

        // เรียกใช้งานสกิลตามคำพูด
        if (actions.ContainsKey(speech.text))
        {
            // ลบ Emoji ออก เพราะ Unity พื้นฐานไม่รองรับ ทำให้ข้อความบัคหรือหาย
            if (micStatusText != null) micStatusText.text = "CASTED: " + speech.text.ToUpper();
            actions[speech.text].Invoke();
            lastSkillTime = Time.time;
        }
    }

    void Update()
    {
        if (isListening && volumeSegments != null && volumeSegments.Length > 0)
        {
            // อัปเดตหลอดระดับเสียง
            float volume = GetMicVolume();
            // ใช้ค่าความไวจาก Inspector (ค่าเริ่มต้นคือ 30f)
            float fillAmount = Mathf.Clamp01(volume * micSensitivity); 

            // คำนวณว่าควรจะให้ขีดสว่างกี่ขีด
            int litCount = Mathf.RoundToInt(fillAmount * segmentCount);

            for (int i = 0; i < segmentCount; i++)
            {
                if (i < litCount)
                {
                    // ช่องที่สว่าง (สีเขียวสว่างเหมือนเกม)
                    volumeSegments[i].color = new Color(0.2f, 0.9f, 0.2f, 1f);
                }
                else
                {
                    // ช่องที่มืด (สีเทาเข้มแบบเห็นชัดๆ)
                    volumeSegments[i].color = new Color(0.2f, 0.2f, 0.2f, 1f);
                }
            }
        }
    }

    private float GetMicVolume()
    {
        if (micClip == null) return 0f;
        int sampleWindow = 128;
        float[] waveData = new float[sampleWindow];
        
        // ใช้ null เพื่อดึงความดังจากไมค์ Default ตัวเดียวกับที่ใช้ร่ายเวทย์
        int micPosition = Microphone.GetPosition(null) - (sampleWindow + 1);
        if (micPosition < 0) return 0;

        micClip.GetData(waveData, micPosition);
        float levelMax = 0;
        for (int i = 0; i < sampleWindow; i++)
        {
            float wavePeak = waveData[i] * waveData[i];
            if (levelMax < wavePeak) levelMax = wavePeak;
        }
        return Mathf.Sqrt(levelMax);
    }

    // --- สร้าง UI อัตโนมัติด้วย Code ---
    private void CreateUIAutomatically()
    {
        // หา Canvas ในฉาก ถ้าไม่มีให้สร้างใหม่
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("AutoCreatedCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // สร้างกรอบพื้นหลัง (Panel)
        micUIPanel = new GameObject("MicPanel_AutoUI");
        micUIPanel.transform.SetParent(canvas.transform, false);
        Image panelImg = micUIPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // สีดำโปร่งแสง

        // จัดให้อยู่มุมล่างซ้าย
        RectTransform panelRect = micUIPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(30, 30); // ห่างจากขอบ
        panelRect.sizeDelta = new Vector2(550, 60);      // ขยายกว้างขึ้นเยอะๆ เผื่อคำว่า CASTED: ...

        // ใส่ HorizontalLayoutGroup เพื่อให้เรียงของอัตโนมัติ (ซ้ายไปขวา)
        HorizontalLayoutGroup layout = micUIPanel.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.spacing = 8;
        layout.padding = new RectOffset(15, 15, 0, 0);

        // 1. สร้างข้อความ "Vol."
        GameObject volTextObj = new GameObject("VolText_AutoUI");
        volTextObj.transform.SetParent(micUIPanel.transform, false);
        Text volText = volTextObj.AddComponent<Text>();
        volText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        volText.fontSize = 20;
        volText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        volText.text = "Vol.";
        volText.alignment = TextAnchor.MiddleLeft;
        volTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);

        // 2. สร้างช่องขีดๆ ของระดับเสียง (Segments)
        volumeSegments = new Image[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segObj = new GameObject("Seg_" + i);
            segObj.transform.SetParent(micUIPanel.transform, false);
            Image segImg = segObj.AddComponent<Image>();
            segImg.color = new Color(0.2f, 0.2f, 0.2f, 1f); // สีเทาเข้ม (เห็นได้ชัดแม้ไม่มีเสียง)
            
            RectTransform segRect = segObj.GetComponent<RectTransform>();
            // ทำเป็นแท่งแนวตั้ง
            segRect.sizeDelta = new Vector2(10, 25); 
            volumeSegments[i] = segImg;
        }

        // 3. สร้างข้อความโชว์สถานะ (เปลี่ยนจากอีโมจิเป็นอักษรเพราะฟอนต์ Unity เก่าไม่รองรับอีโมจิ)
        GameObject statusTextObj = new GameObject("StatusText_AutoUI");
        statusTextObj.transform.SetParent(micUIPanel.transform, false);
        micStatusText = statusTextObj.AddComponent<Text>();
        micStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        micStatusText.fontSize = 20;
        micStatusText.color = new Color(0.5f, 1f, 0.5f, 1f); // สีเขียวอ่อน
        micStatusText.text = "MIC ON";
        micStatusText.alignment = TextAnchor.MiddleLeft;
        statusTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40); // ขยายความกว้างข้อความเป็น 200

        // ปิดการแสดงผลไว้ก่อนตั้งแต่เริ่ม
        micUIPanel.SetActive(false);
    }

    // --- SKILLS IMPLEMENTATION ---
    private void FireSkill()
    {
        ApplyDamageAndEffect(fireDamage, KnockbackType.None, 0f, fireEffect);
    }

    private void PushSkill()
    {
        ApplyDamageAndEffect(pushDamage, KnockbackType.Pushback, 10f, pushEffect);
    }

    private void BoomSkill()
    {
        ApplyDamageAndEffect(boomDamage, KnockbackType.Knockdown, 15f, boomEffect);
    }

    // ฟังก์ชันรวบรวมการทำดาเมจและเรียกเอฟเฟกต์ให้ถูกจุด
    private void ApplyDamageAndEffect(int damage, KnockbackType kbType, float kbPower, ParticleSystem effectPrefab)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, skillRadius);
        bool hitSomeone = false;

        foreach (var hit in hits)
        {
            var ai = hit.GetComponent<OpponentAI>();
            if (ai != null)
            {
                hitSomeone = true;
                
                // 1. สร้างเอฟเฟกต์ระเบิด/แสง ตรงจุดที่ศัตรูยืนอยู่ (ยกขึ้นมา 1 หน่วยให้ตรงกลางตัว)
                PlayEffectAtPosition(effectPrefab, hit.transform.position + Vector3.up);

                // 2. คำนวณทิศทางให้กระเด็นออกจากตัว Player
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                dir.y = 0;
                
                ai.StartCoroutine(ai.PlayHitDamageAnimation(damage, kbType, dir, kbPower));
            }
        }

        // ถ้าร่ายเวทย์แล้วไม่โดนใครเลย (วืด) ให้แสงไปโผล่ข้างหน้าผู้เล่นนิดนึงแทน (จะได้รู้ว่าสกิลออกแล้ว)
        if (!hitSomeone)
        {
            Vector3 frontPosition = transform.position + (transform.forward * 2f) + Vector3.up;
            PlayEffectAtPosition(effectPrefab, frontPosition);
        }
    }

    // ฟังก์ชันสำหรับเล่น Effect ตรงพิกัดที่กำหนด
    private void PlayEffectAtPosition(ParticleSystem effectPrefab, Vector3 spawnPosition)
    {
        if (effectPrefab == null) return;

        ParticleSystem newEffect = Instantiate(effectPrefab, spawnPosition, Quaternion.identity);
        newEffect.gameObject.SetActive(true);
        newEffect.Play();

        float destroyTime = newEffect.main.duration + newEffect.main.startLifetime.constantMax;
        Destroy(newEffect.gameObject, destroyTime > 0 ? destroyTime : 3f);
    }

    void OnDestroy()
    {
        if (keywordRecognizer != null)
        {
            keywordRecognizer.Stop();
            keywordRecognizer.Dispose();
        }
    }
}
