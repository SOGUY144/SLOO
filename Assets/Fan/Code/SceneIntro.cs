using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneIntro : MonoBehaviour
{
    [Header("References")]
    public RawImage dissolveOverlay;
    public Material dissolveMaterial;

    [Header("Settings")]
    [Tooltip("ความเร็วตอน dissolve เข้าตอนเริ่ม scene (วินาที)")]
    public float introDuration = 1.0f;
    public string dissolvePropertyName = "_Progress";

    private int DissolveID;
    private Material _matInstance;

    void Awake()
    {
        DissolveID = Shader.PropertyToID(dissolvePropertyName);
        _matInstance = new Material(dissolveMaterial);
        dissolveOverlay.material = _matInstance;
        _matInstance.SetFloat(DissolveID, 1f);
        dissolveOverlay.gameObject.SetActive(false);
    }

    IEnumerator Start()
    {
        dissolveOverlay.gameObject.SetActive(true);
        _matInstance.SetFloat(DissolveID, 1f);

        // Dissolve IN: 1 → 0
        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / introDuration);
            _matInstance.SetFloat(DissolveID, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
        _matInstance.SetFloat(DissolveID, 0f);
        dissolveOverlay.gameObject.SetActive(false);
    }
}