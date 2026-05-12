using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private AudioSource[] bgmSources;

    [Header("SFX")]
    [SerializeField] private AudioSource[] sfxSources;

    private const string BGM_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";
    private const float DEFAULT_VALUE = 100f;

    private void Awake()
    {
        // โหลดค่าเสียงทันทีตอนเริ่ม ก่อน Start ของ Script อื่น
        float bgmVolume = PlayerPrefs.GetFloat(BGM_KEY, DEFAULT_VALUE) / 100f;
        float sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, DEFAULT_VALUE) / 100f;

        foreach (AudioSource src in bgmSources)
            if (src != null) src.volume = bgmVolume;

        foreach (AudioSource src in sfxSources)
            if (src != null) src.volume = sfxVolume;
    }
}