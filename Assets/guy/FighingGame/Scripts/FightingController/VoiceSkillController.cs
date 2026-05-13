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
    [Tooltip("จุดที่ถือว่าเป็นการตะโกน (0.0 ถึง 1.0)")]
    public float shoutThreshold = 0.8f;

    // ตั้งค่าหลอดวัดเสียงแบบจุดๆ (Segment)
    public int segmentCount = 15;
    private UnityEngine.UI.Image[] volumeSegments;

    private bool isListening = false;
    private bool isShouting = false; // ตัวแปรเก็บสถานะว่าตะโกนอยู่ไหมตอนร่ายเวทย์
    private bool wasShoutingRecently = false; // เก็บความทรงจำว่าเพิ่งตะโกนไป
    private float peakVolumeTimer = 0f;
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
            // เช็คว่าใน 1.5 วินาทีที่ผ่านมา มีจังหวะไหนที่หลอดเสียงทะลุหลอดบ้างไหม
            isShouting = wasShoutingRecently;

            // รีเซ็ตเพื่อไม่ให้ร่ายเวทย์ครั้งต่อไปติด Super ฟรีๆ
            wasShoutingRecently = false;
            peakVolumeTimer = 0f;

            // อัปเดต UI ให้รู้ว่าร่ายแบบปกติ หรือแบบตะโกน
            if (micStatusText != null) 
            {
                if (isShouting)
                {
                    micStatusText.text = "SUPER " + speech.text.ToUpper() + "!!!";
                    micStatusText.color = new Color(1f, 0.4f, 0.2f, 1f); // สีส้มแดง (คริติคอล)
                }
                else
                {
                    micStatusText.text = "CASTED: " + speech.text.ToUpper();
                    micStatusText.color = new Color(0.5f, 1f, 0.5f, 1f); // สีเขียวปกติ
                }
            }

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
            float fillAmount = Mathf.Clamp01(volume * micSensitivity); 

            // จำไว้ว่าเพิ่งตะโกน (หน่วงเวลาไว้ 1.5 วินาที เพราะกว่าระบบพูดจะจับคำได้ เสียงเรามักจะเงียบไปแล้ว)
            if (fillAmount >= shoutThreshold)
            {
                wasShoutingRecently = true;
                peakVolumeTimer = 1.5f;
            }

            if (peakVolumeTimer > 0)
            {
                peakVolumeTimer -= Time.deltaTime;
                if (peakVolumeTimer <= 0) wasShoutingRecently = false;
            }

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
        // 1. ตรวจสอบว่าเป็นการ "ตะโกน" หรือไม่ ถ้าใช่ให้อัปเกรดสกิล!
        int finalDamage = damage;
        KnockbackType finalKbType = kbType;
        float finalKbPower = kbPower;
        float effectScale = 1f;

        if (isShouting)
        {
            finalDamage = damage * 2;          // ดาเมจแรงขึ้น 2 เท่า
            effectScale = 2f;                  // เอฟเฟกต์ใหญ่ขึ้น 2 เท่า
            finalKbPower = kbPower * 1.5f;     // กระเด็นแรงขึ้น 1.5 เท่า

            // เลื่อนขั้นสถานะการกระเด็นให้รุนแรงขึ้น
            if (kbType == KnockbackType.None) finalKbType = KnockbackType.Pushback;
            else if (kbType == KnockbackType.Pushback) finalKbType = KnockbackType.Knockdown;

            // สั่นกล้องเพิ่มความสะใจ
            StartCoroutine(ShakeCamera(0.2f, 0.4f));
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, skillRadius);
        bool hitSomeone = false;

        foreach (var hit in hits)
        {
            var ai = hit.GetComponent<OpponentAI>();
            if (ai != null)
            {
                hitSomeone = true;
                
                // 1. สร้างเอฟเฟกต์ระเบิด/แสง ตรงจุดที่ศัตรูยืนอยู่ พร้อมขยายขนาดถ้าตะโกน
                PlayEffectAtPosition(effectPrefab, hit.transform.position + Vector3.up, effectScale);

                // 2. คำนวณทิศทางให้กระเด็นออกจากตัว Player
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                dir.y = 0;
                
                ai.StartCoroutine(ai.PlayHitDamageAnimation(finalDamage, finalKbType, dir, finalKbPower));
            }
        }

        // ถ้าร่ายเวทย์แล้วไม่โดนใครเลย (วืด) ให้แสงไปโผล่ข้างหน้าผู้เล่น
        if (!hitSomeone)
        {
            Vector3 frontPosition = transform.position + (transform.forward * 2f) + Vector3.up;
            PlayEffectAtPosition(effectPrefab, frontPosition, effectScale);
        }
    }

    // ฟังก์ชันสำหรับเล่น Effect ตรงพิกัดที่กำหนด พร้อมปรับขนาด
    private void PlayEffectAtPosition(ParticleSystem effectPrefab, Vector3 spawnPosition, float scaleMultiplier)
    {
        if (effectPrefab == null) return;

        ParticleSystem newEffect = Instantiate(effectPrefab, spawnPosition, Quaternion.identity);
        
        // ถ้ามีการตะโกน (สเกลไม่ใช่ 1) ให้ขยายขนาด Effect
        if (scaleMultiplier != 1f)
        {
            newEffect.transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);
        }

        newEffect.gameObject.SetActive(true);
        newEffect.Play();

        float destroyTime = newEffect.main.duration + newEffect.main.startLifetime.constantMax;
        Destroy(newEffect.gameObject, destroyTime > 0 ? destroyTime : 3f);
    }

    // ฟังก์ชันทำกล้องสั่น (Camera Shake)
    private System.Collections.IEnumerator ShakeCamera(float duration, float magnitude)
    {
        if (Camera.main == null) yield break;
        Vector3 originalPos = Camera.main.transform.localPosition;
        float elapsed = 0.0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            Camera.main.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Camera.main.transform.localPosition = originalPos;
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
