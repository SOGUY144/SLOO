using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;

    [Header("References")]
    public RawImage dissolveOverlay;
    public Material dissolveMaterial;

    [Header("Settings")]
    [Tooltip("ความเร็วตอน dissolve ออก (วินาที)")]
    public float durationOut = 1.0f;
    [Tooltip("ความเร็วตอน dissolve เข้า (วินาที)")]
    public float durationIn = 1.0f;
    public string dissolvePropertyName = "_Progress";

    private int DissolveID;
    private Material _matInstance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DissolveID = Shader.PropertyToID(dissolvePropertyName);
        _matInstance = new Material(dissolveMaterial);
        dissolveOverlay.material = _matInstance;

        // Debug property ทั้งหมด
        Shader shader = _matInstance.shader;
        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            Debug.Log("Property " + i + ": " + shader.GetPropertyName(i));
        }

        _matInstance.SetFloat(DissolveID, 0f);
        dissolveOverlay.gameObject.SetActive(false);
    }

    // เรียกปกติ ใช้ค่า duration จาก Inspector
    public void GoToScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName, durationOut, durationIn));
    }

    // เรียกแบบกำหนด duration เองได้
    public void GoToScene(string sceneName, float outDuration, float inDuration)
    {
        StartCoroutine(TransitionRoutine(sceneName, outDuration, inDuration));
    }

    public void RestartScene()
    {
        StartCoroutine(TransitionRoutine(SceneManager.GetActiveScene().name, durationOut, durationIn));
    }

    public void RestartScene(float outDuration, float inDuration)
    {
        StartCoroutine(TransitionRoutine(SceneManager.GetActiveScene().name, outDuration, inDuration));
    }

    public void GoToMenu()
    {
        StartCoroutine(TransitionRoutine("Mainmenu", durationOut, durationIn));
    }

    public void GoToMenu(float outDuration, float inDuration)
    {
        StartCoroutine(TransitionRoutine("Mainmenu", outDuration, inDuration));
    }

    IEnumerator TransitionRoutine(string sceneName, float outDur, float inDur)
    {
        dissolveOverlay.gameObject.SetActive(true);

        // Dissolve OUT: 0 → 1
        yield return StartCoroutine(Animate(0f, 1f, outDur));

        // โหลด scene
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;
        yield return null;
        yield return null;

        // Dissolve IN: 1 → 0
        yield return StartCoroutine(Animate(1f, 0f, inDur));

        dissolveOverlay.gameObject.SetActive(false);
    }

    IEnumerator Animate(float from, float to, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
            _matInstance.SetFloat(DissolveID, Mathf.Lerp(from, to, t));
            yield return null;
        }
        _matInstance.SetFloat(DissolveID, to);
    }
}