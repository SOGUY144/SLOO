using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio; // เพิ่มอันนี้เพื่อใช้ Mixer
using TMPro;

public class SettingManager : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer mainMixer; // ลากไฟล์ Audio Mixer มาใส่ที่นี่

    [Header("BGM")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmValueText;

    [Header("SFX")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxValueText;

    [Header("Reset Value")]
    [SerializeField] private float defaultValue = 100f;

    [Header("Panels")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject mainMenuPanel;

    private const string BGM_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";

    private void Start()
    {
        float savedBGM = PlayerPrefs.GetFloat(BGM_KEY, defaultValue);
        float savedSFX = PlayerPrefs.GetFloat(SFX_KEY, defaultValue);

        bgmSlider.minValue = 0.0001f; // ห้ามเป็น 0 เพราะจะคำนวณสูตร Log ไม่ได้
        bgmSlider.maxValue = 100f;
        sfxSlider.minValue = 0.0001f;
        sfxSlider.maxValue = 100f;

        bgmSlider.value = savedBGM;
        sfxSlider.value = savedSFX;

        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);

        UpdateBGM(savedBGM);
        UpdateSFX(savedSFX);
    }

    private void OnBGMChanged(float value)
    {
        UpdateBGM(value);
        PlayerPrefs.SetFloat(BGM_KEY, value);
        PlayerPrefs.Save();
    }

    private void OnSFXChanged(float value)
    {
        UpdateSFX(value);
        PlayerPrefs.SetFloat(SFX_KEY, value);
        PlayerPrefs.Save();
    }

    private void UpdateBGM(float value)
    {
        // คำนวณค่าเป็น Decibel เพื่อส่งให้ Mixer
        float dB = Mathf.Log10(value / 100f) * 20;
        mainMixer.SetFloat("BGMVolume", dB);

        if (bgmValueText != null)
            bgmValueText.text = Mathf.RoundToInt(value).ToString();
    }

    private void UpdateSFX(float value)
    {
        float dB = Mathf.Log10(value / 100f) * 20;
        mainMixer.SetFloat("SFXVolume", dB);

        if (sfxValueText != null)
            sfxValueText.text = Mathf.RoundToInt(value).ToString();
    }

    public void OnClickReset()
    {
        bgmSlider.value = defaultValue;
        sfxSlider.value = defaultValue;
    }

    public void OnClickBack()
    {
        settingPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}