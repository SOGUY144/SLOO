using UnityEngine;

public class SkyboxExposureIntro : MonoBehaviour
{
    // สร้างเป็น Singleton เพื่อให้สคริปต์อื่น (เช่น ศัตรู) สามารถมาถามเวลาจากตัวนี้ได้ง่ายๆ
    public static SkyboxExposureIntro Instance { get; private set; }

    [Header("Day & Night Settings")]
    [Tooltip("ค่า Exposure ตอนเช้า (สว่าง)")]
    public float dayExposure = 1.0f;

    [Tooltip("ค่า Exposure ตอนกลางคืน (มืด)")]
    public float nightExposure = 0.2f;

    [Tooltip("1 วันในเกม ใช้เวลากี่วินาทีของจริง")]
    public float fullDayDuration = 60f;

    [Tooltip("สถานะปัจจุบัน (จริง = กลางคืน, เท็จ = กลางวัน)")]
    public bool isNight = false;

    private Material _skyboxMat;
    private float currentTime = 0f;

    void Awake()
    {
        // ผูก Instance ให้ตัวอื่นเรียกใช้ได้ผ่าน SkyboxExposureIntro.Instance
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _skyboxMat = RenderSettings.skybox;

        // เริ่มต้นตอนเช้า
        if (_skyboxMat != null)
            _skyboxMat.SetFloat("_Exposure", dayExposure);
    }

    void Update()
    {
        if (_skyboxMat == null) return;

        // เดินเวลาไปเรื่อยๆ
        currentTime += Time.deltaTime;
        
        // ถ้านับเวลาครบ 1 วัน ให้รีเซ็ตกลับไปเป็น 0 (เริ่มวันใหม่)
        if (currentTime >= fullDayDuration)
        {
            currentTime = 0f;
        }

        // คำนวณสัดส่วนเวลา 0.0 - 1.0
        float timeRatio = currentTime / fullDayDuration;

        // ใช้สูตร PingPong เพื่อให้เวลา 0.0->0.5 สว่างไปมืด และ 0.5->1.0 มืดไปสว่าง
        float exposureLerp = Mathf.PingPong(timeRatio * 2f, 1f); 

        // ทำให้การเปลี่ยนแสงนุ่มนวลขึ้น
        float smoothT = Mathf.SmoothStep(0f, 1f, exposureLerp);
        
        // อัปเดตแสงบนท้องฟ้า
        _skyboxMat.SetFloat("_Exposure", Mathf.Lerp(dayExposure, nightExposure, smoothT));

        // --- ระบบเช็คว่ามืดแล้วหรือยัง ---
        // ถ้าค่า timeRatio อยู่ช่วง 0.3 ถึง 0.7 จะถือว่าเป็นตอนกลางคืน (มืดเกิน 60%)
        bool wasNight = isNight;
        isNight = (timeRatio > 0.3f && timeRatio < 0.7f);

        // แสดงแจ้งเตือนแค่ตอนที่เกิดการเปลี่ยนผ่านจริงๆ
        if (isNight && !wasNight) Debug.Log("🌙 พระอาทิตย์ตกแล้ว... ศัตรูเริ่มแข็งแกร่งขึ้น!");
        if (!isNight && wasNight) Debug.Log("☀️ พระอาทิตย์ขึ้น! ศัตรูกลับสู่สภาพปกติ");
    }

    void OnDestroy()
    {
        // รีเซ็ตแสงกลับเป็นตอนเช้าเสมอเวลาออกจากด่าน
        if (_skyboxMat != null)
        {
            _skyboxMat.SetFloat("_Exposure", dayExposure);
        }
    }
}