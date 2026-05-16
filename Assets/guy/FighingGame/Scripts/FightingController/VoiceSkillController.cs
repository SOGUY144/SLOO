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

    [Header("Audio Settings (เสียงสกิล)")]
    public AudioClip fireSound;
    public AudioClip pushSound;
    public AudioClip boomSound;
    [Tooltip("เสียงที่จะแทรกเข้ามาเพิ่มตอนร่ายแบบตะโกน (SUPER)")]
    public AudioClip superSound;

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

    [Header("Speed/Combo Mechanics")]
    public float pulseThreshold = 0.6f; // ต้องดังแค่ไหนถึงจะนับ 1 จังหวะ
    public float pulseDropThreshold = 0.2f; // ต้องเบาลงแค่ไหนถึงจะเริ่มนับจังหวะใหม่ได้
    
    public int maxComboPoints = 20; // แต้มสะสมสูงสุด
    public int comboForLevel2 = 8;  // แต้มที่ต้องใช้สำหรับ LV2
    public int comboForLevel3 = 16; // แต้มที่ต้องใช้สำหรับ LV3
    public float pointDecayTimer = 3.0f; // หยุดพูดกี่วินาที แต้มถึงจะเริ่มลด (ไม่เหนื่อยแล้ว!)
    
    private int currentComboPoints = 0;
    private float lastPulseTime = 0f;
    private bool canCountPulse = true;
    public int currentSpeedLevel { get; private set; } = 1; // 1=Normal, 2=Fast, 3=Max

    // สำหรับ UI คอมโบแยก
    private GameObject comboUIPanel;
    private UnityEngine.UI.Text comboStatusText;
    private UnityEngine.UI.Image[] comboSegments;

    void Start()
    {
        // สร้าง UI อัตโนมัติถ้ายังไม่ได้ลากใส่ช่อง
        if (micUIPanel == null || micStatusText == null)
        {
            CreateUIAutomatically();
        }

        // สร้างหลอดคอมโบซ้อนไว้ด้านบน (แบบไม่ยุ่งกับ UI เดิม)
        if (comboUIPanel == null)
        {
            CreateComboUIAutomatically();
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
            
            // เปิด UI ไมโครโฟนและคอมโบ
            if (micUIPanel != null) micUIPanel.SetActive(true);
            if (comboUIPanel != null) comboUIPanel.SetActive(true);
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
            
            // ปิด UI ไมโครโฟนและคอมโบ
            if (micUIPanel != null) micUIPanel.SetActive(false);
            if (comboUIPanel != null) comboUIPanel.SetActive(false);
            
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
            // เช็คว่าตอนที่พูดร่ายเวทย์ มีการตะโกนเสียงดังเกินเพดานหรือไม่
            isShouting = wasShoutingRecently;
            wasShoutingRecently = false; // รีเซ็ตเพื่อไม่ให้ร่ายครั้งหน้าติดไปด้วย
            peakVolumeTimer = 0f;

            // อัปเดต UI ให้รู้ว่าร่ายแบบระดับไหนและตะโกนหรือไม่
            if (micStatusText != null) 
            {
                string shoutPrefix = isShouting ? "SUPER " : "";
                
                if (currentSpeedLevel >= 3)
                {
                    micStatusText.text = shoutPrefix + "MAX " + speech.text.ToUpper() + "!!!";
                    micStatusText.color = new Color(1f, 0.2f, 0.2f, 1f); // สีแดง
                }
                else if (currentSpeedLevel == 2)
                {
                    micStatusText.text = shoutPrefix + "DOUBLE " + speech.text.ToUpper() + "!";
                    micStatusText.color = new Color(1f, 0.8f, 0f, 1f); // สีเหลือง
                }
                else
                {
                    if (isShouting)
                    {
                        micStatusText.text = "SUPER " + speech.text.ToUpper() + "!!!";
                        micStatusText.color = new Color(1f, 0.4f, 0.2f, 1f); // สีส้มแดง
                    }
                    else
                    {
                        micStatusText.text = "CASTED: " + speech.text.ToUpper();
                        micStatusText.color = new Color(0.5f, 1f, 0.5f, 1f); // สีเขียวปกติ
                    }
                }
            }

            actions[speech.text].Invoke();
            
            // รีเซ็ตเกจความเร็วและจังหวะหลังร่ายเวทย์เสร็จ (เพื่อให้ผู้เล่นต้องเริ่มรัวใหม่ในครั้งหน้า)
            currentComboPoints = 0;
            currentSpeedLevel = 1;
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

            // --- [STEP 1: ระบบสะสมแต้ม (Energy Gauge)] ---
            if (canCountPulse && fillAmount >= pulseThreshold)
            {
                currentComboPoints++;
                if (currentComboPoints > maxComboPoints) currentComboPoints = maxComboPoints;

                lastPulseTime = Time.time;
                canCountPulse = false; // ป้องกันนับซ้ำในเสียงลากยาว
                
                Debug.Log($"🔋 ชาร์จพลัง! แต้มสะสม: {currentComboPoints}/{maxComboPoints} | ความเร็ว LV.{currentSpeedLevel}");
            }
            else if (!canCountPulse && fillAmount <= pulseDropThreshold)
            {
                canCountPulse = true; // รีเซ็ตเพื่อรับเสียงกระแทกครั้งต่อไป
            }

            // ถอยหลังแต้มลงทีละ 1 ถ้าหยุดพูดนานเกิน (เช่น 3 วินาที)
            if (currentComboPoints > 0 && Time.time - lastPulseTime > pointDecayTimer)
            {
                currentComboPoints--;
                lastPulseTime = Time.time; // รีเซ็ตเพื่อหน่วงเวลาการลดแต้มต่อไป 
                Debug.Log($"⚠️ พลังลดลงเล็กน้อย! แต้มสะสมเหลือ: {currentComboPoints}");
            }

            // คำนวณเลเวลความเร็วปัจจุบันจากแต้มสะสม
            if (currentComboPoints >= comboForLevel3) currentSpeedLevel = 3;
            else if (currentComboPoints >= comboForLevel2) currentSpeedLevel = 2;
            else currentSpeedLevel = 1;
            // -----------------------------------------------------------

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

            // --- [STEP 2: เปลี่ยนสีหลอดตาม Speed Level] ---
            Color activeColor = new Color(0.2f, 0.9f, 0.2f, 1f); // สีเขียวปกติ (LV 1)
            if (currentSpeedLevel == 2) activeColor = new Color(1f, 0.8f, 0f, 1f); // สีเหลือง/ส้ม (LV 2)
            else if (currentSpeedLevel >= 3) activeColor = new Color(1f, 0.2f, 0.2f, 1f); // สีแดงเพลิง (LV 3)

            // คำนวณว่าควรจะให้ขีดสว่างกี่ขีด
            int litCount = Mathf.RoundToInt(fillAmount * segmentCount);

            for (int i = 0; i < segmentCount; i++)
            {
                if (i < litCount)
                {
                    // ช่องที่สว่าง (สีเปลี่ยนตามเลเวลความเร็ว)
                    volumeSegments[i].color = activeColor;
                }
                else
                {
                    // ช่องที่มืด (สีเทาเข้มแบบเห็นชัดๆ)
                    volumeSegments[i].color = new Color(0.2f, 0.2f, 0.2f, 1f);
                }
            }

            // --- [อัปเดต UI หลอดคอมโบด้านบน] ---
            if (comboSegments != null && comboSegments.Length == maxComboPoints)
            {
                if (comboStatusText != null)
                {
                    if (currentSpeedLevel == 3) comboStatusText.text = "MAX POWER!";
                    else if (currentSpeedLevel == 2) comboStatusText.text = "LV.2 FAST!";
                    else comboStatusText.text = "COMBO";
                    comboStatusText.color = activeColor;
                }

                for (int i = 0; i < maxComboPoints; i++)
                {
                    if (i < currentComboPoints)
                    {
                        comboSegments[i].color = activeColor; // สว่างตามสี LV
                    }
                    else
                    {
                        comboSegments[i].color = new Color(0.1f, 0.1f, 0.1f, 1f); // มืด
                    }
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

    // --- สร้าง UI หลอดคอมโบแยกต่างหาก (ไม่ยุ่งกับ UI เดิม) ---
    private void CreateComboUIAutomatically()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return; 

        comboUIPanel = new GameObject("ComboPanel_AutoUI");
        comboUIPanel.transform.SetParent(canvas.transform, false);
        Image comboPanelImg = comboUIPanel.AddComponent<Image>();
        comboPanelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        RectTransform comboRect = comboUIPanel.GetComponent<RectTransform>();
        comboRect.anchorMin = new Vector2(0, 0);
        comboRect.anchorMax = new Vector2(0, 0);
        comboRect.pivot = new Vector2(0, 0);
        
        // วางซ้อนด้านบนหลอดไมค์ (หลอดไมค์เดิมอยู่ที่ Y=30 ความสูง=60 ดังนั้นวางคอมโบไว้ที่ Y=100)
        comboRect.anchoredPosition = new Vector2(30, 100); 
        comboRect.sizeDelta = new Vector2(550, 40); // ความสูงน้อยกว่าหลอดไมค์

        HorizontalLayoutGroup comboLayout = comboUIPanel.AddComponent<HorizontalLayoutGroup>();
        comboLayout.childAlignment = TextAnchor.MiddleLeft;
        comboLayout.childControlHeight = false;
        comboLayout.childControlWidth = false;
        comboLayout.spacing = 4; // ลดช่องว่างลงนิดนึง
        comboLayout.padding = new RectOffset(15, 15, 0, 0);

        // ข้อความ
        GameObject comboTextObj = new GameObject("ComboText_AutoUI");
        comboTextObj.transform.SetParent(comboUIPanel.transform, false);
        comboStatusText = comboTextObj.AddComponent<Text>();
        comboStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        comboStatusText.fontSize = 16;
        comboStatusText.color = new Color(1f, 1f, 1f, 1f);
        comboStatusText.text = "COMBO";
        comboStatusText.alignment = TextAnchor.MiddleLeft;
        comboTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40); // ลดความกว้างกล่องข้อความลง

        // ช่องคอมโบ 20 ช่อง
        comboSegments = new Image[maxComboPoints];
        for (int i = 0; i < maxComboPoints; i++)
        {
            GameObject cSegObj = new GameObject("CSeg_" + i);
            cSegObj.transform.SetParent(comboUIPanel.transform, false);
            Image cSegImg = cSegObj.AddComponent<Image>();
            cSegImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            RectTransform cSegRect = cSegObj.GetComponent<RectTransform>();
            cSegRect.sizeDelta = new Vector2(14, 20); // ลดความกว้างแต่ละช่องลง จะได้ไม่ทะลุกรอบ
            comboSegments[i] = cSegImg;
        }
        
        comboUIPanel.SetActive(false);
    }

    // --- SKILLS IMPLEMENTATION ---
    private void FireSkill()
    {
        ApplyDamageAndEffect(fireDamage, KnockbackType.None, 0f, fireEffect, fireSound);
    }

    private void PushSkill()
    {
        ApplyDamageAndEffect(pushDamage, KnockbackType.Pushback, 10f, pushEffect, pushSound);
    }

    private void BoomSkill()
    {
        ApplyDamageAndEffect(boomDamage, KnockbackType.Knockdown, 15f, boomEffect, boomSound);
    }

    // ฟังก์ชันรวบรวมการทำดาเมจและเรียกเอฟเฟกต์ให้ถูกจุด
    private void ApplyDamageAndEffect(int damage, KnockbackType kbType, float kbPower, ParticleSystem effectPrefab, AudioClip skillSound)
    {
        // เล่นเสียงพื้นฐานของสกิล
        if (skillSound != null)
        {
            AudioSource.PlayClipAtPoint(skillSound, transform.position);
        }

        int finalDamage = damage;
        KnockbackType finalKbType = kbType;
        float finalKbPower = kbPower;
        float effectScale = 1f;

        // 1. ตรวจสอบว่า "ตะโกนสุดเสียง" หรือไม่ (เพื่ออัปเกรดความรุนแรงและขนาด)
        if (isShouting)
        {
            finalDamage = damage * 2;          // ดาเมจ x2
            effectScale = 2f;                  // ลูกระเบิดใหญ่ขึ้น 2 เท่า
            finalKbPower = kbPower * 1.5f;     // กระเด็นแรงขึ้น
            
            if (kbType == KnockbackType.None) finalKbType = KnockbackType.Pushback;
            else if (kbType == KnockbackType.Pushback) finalKbType = KnockbackType.Knockdown;

            if (superSound != null) AudioSource.PlayClipAtPoint(superSound, transform.position);
            StartCoroutine(ShakeCamera(0.4f, 0.8f)); // กล้องสั่นแรง
        }
        else
        {
            StartCoroutine(ShakeCamera(0.15f, 0.2f)); // กล้องสั่นปกติ
        }

        // 2. ตรวจสอบ "Combo Level" เพื่อหาจำนวนลูกที่ต้องยิง
        int projectileCount = 1; // เริ่มต้นยิง 1 ลูก
        if (currentSpeedLevel == 2) projectileCount = 2; // คอมโบกลาง ยิง 2 ลูก
        else if (currentSpeedLevel >= 3) projectileCount = 3; // คอมโบเต็ม ยิง 3 ลูก

        Collider[] hits = Physics.OverlapSphere(transform.position, skillRadius);
        bool hitSomeone = false;

        foreach (var hit in hits)
        {
            var ai = hit.GetComponent<OpponentAI>();
            if (ai != null)
            {
                hitSomeone = true;
                
                // สร้างเอฟเฟกต์ตามจำนวน Projectile Count
                Vector3 targetPos = hit.transform.position + Vector3.up;
                
                if (projectileCount == 3)
                {
                    PlayEffectAtPosition(effectPrefab, targetPos, effectScale);
                    PlayEffectAtPosition(effectPrefab, targetPos + transform.right * 2f, effectScale);
                    PlayEffectAtPosition(effectPrefab, targetPos - transform.right * 2f, effectScale);
                }
                else if (projectileCount == 2)
                {
                    PlayEffectAtPosition(effectPrefab, targetPos + transform.right * 1.5f, effectScale);
                    PlayEffectAtPosition(effectPrefab, targetPos - transform.right * 1.5f, effectScale);
                }
                else
                {
                    PlayEffectAtPosition(effectPrefab, targetPos, effectScale);
                }

                // คำนวณทิศทางให้กระเด็นออกจากตัว Player
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                dir.y = 0;
                
                ai.StartCoroutine(ai.PlayHitDamageAnimation(finalDamage, finalKbType, dir, finalKbPower));
            }
        }

        // ถ้าร่ายเวทย์แล้วไม่โดนใครเลย (วืด) ให้แสงไปโผล่ข้างหน้าผู้เล่น
        if (!hitSomeone)
        {
            Vector3 frontPos = transform.position + (transform.forward * 2f) + Vector3.up;
            
            if (projectileCount == 3)
            {
                PlayEffectAtPosition(effectPrefab, frontPos, effectScale);
                PlayEffectAtPosition(effectPrefab, frontPos + transform.right * 2f, effectScale);
                PlayEffectAtPosition(effectPrefab, frontPos - transform.right * 2f, effectScale);
            }
            else if (projectileCount == 2)
            {
                PlayEffectAtPosition(effectPrefab, frontPos + transform.right * 1.5f, effectScale);
                PlayEffectAtPosition(effectPrefab, frontPos - transform.right * 1.5f, effectScale);
            }
            else
            {
                PlayEffectAtPosition(effectPrefab, frontPos, effectScale);
            }
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
