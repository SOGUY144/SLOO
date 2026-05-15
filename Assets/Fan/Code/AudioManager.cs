using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixer mainMixer;

    private const string BGM_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";
    private const float DEFAULT_VALUE = 100f;

    private void Start()
    {
        // โหลดค่ามาเซ็ตให้ Mixer ตอนเริ่มเข้าเกม
        float bgm = PlayerPrefs.GetFloat(BGM_KEY, DEFAULT_VALUE);
        float sfx = PlayerPrefs.GetFloat(SFX_KEY, DEFAULT_VALUE);

        mainMixer.SetFloat("BGMVolume", Mathf.Log10(bgm / 100f) * 20);
        mainMixer.SetFloat("SFXVolume", Mathf.Log10(sfx / 100f) * 20);
    }
}