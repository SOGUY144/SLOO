using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingManager : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private AudioSource[] bgmSources;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmValueText;

    [Header("SFX")]
    [SerializeField] private AudioSource[] sfxSources;
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

        bgmSlider.minValue = 0;
        bgmSlider.maxValue = 100;
        sfxSlider.minValue = 0;
        sfxSlider.maxValue = 100;

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
        foreach (AudioSource src in bgmSources)
            if (src != null) src.volume = value / 100f;

        if (bgmValueText != null)
            bgmValueText.text = Mathf.RoundToInt(value).ToString();
    }

    private void UpdateSFX(float value)
    {
        foreach (AudioSource src in sfxSources)
            if (src != null) src.volume = value / 100f;

        if (sfxValueText != null)
            sfxValueText.text = Mathf.RoundToInt(value).ToString();
    }

    // ปุ่ม RESET
    public void OnClickReset()
    {
        bgmSlider.value = defaultValue;
        sfxSlider.value = defaultValue;
        PlayerPrefs.SetFloat(BGM_KEY, defaultValue);
        PlayerPrefs.SetFloat(SFX_KEY, defaultValue);
        PlayerPrefs.Save();
    }

    // ปุ่ม BACK
    public void OnClickBack()
    {
        settingPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}