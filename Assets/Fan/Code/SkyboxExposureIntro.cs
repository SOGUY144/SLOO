using System.Collections;
using UnityEngine;

public class SkyboxExposureIntro : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("ค่า Exposure ที่เริ่มต้น")]
    public float startExposure = 1.0f;

    [Tooltip("ค่า Exposure ที่จะไปถึง")]
    public float targetExposure = 0f;

    [Tooltip("ใช้เวลากี่วินาที")]
    public float duration = 2.0f;

    [Tooltip("หน่วงก่อนเริ่มกี่วินาที")]
    public float delay = 0f;

    private Material _skyboxMat;

    void Awake()
    {
        _skyboxMat = RenderSettings.skybox;

        if (_skyboxMat != null)
            _skyboxMat.SetFloat("_Exposure", startExposure);
    }

    IEnumerator Start()
    {
        if (_skyboxMat == null)
        {
            Debug.LogWarning("SkyboxExposureIntro: ไม่พบ Skybox Material ใน RenderSettings");
            yield break;
        }

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _skyboxMat.SetFloat("_Exposure", Mathf.Lerp(startExposure, targetExposure, t));
            yield return null;
        }

        _skyboxMat.SetFloat("_Exposure", targetExposure);
    }
}