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

    private void OnEnable()
    {
        // กัน Time.timeScale ค้างจาก ResultManager
        Time.timeScale = 1f;
    }

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

        _matInstance.SetFloat(DissolveID, 0f);

        dissolveOverlay.gameObject.SetActive(false);
    }

    public void GoToScene(string sceneName)
    {
        Debug.Log("GoToScene Called : " + sceneName);

        // กันเกมค้างจาก Time.timeScale = 0
        Time.timeScale = 1f;

        StartCoroutine(TransitionRoutine(sceneName, durationOut, durationIn));
    }

    public void GoToScene(string sceneName, float outDuration, float inDuration)
    {
        Time.timeScale = 1f;

        StartCoroutine(TransitionRoutine(sceneName, outDuration, inDuration));
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;

        StartCoroutine(TransitionRoutine(SceneManager.GetActiveScene().name, durationOut, durationIn));
    }

    public void RestartScene(float outDuration, float inDuration)
    {
        Time.timeScale = 1f;

        StartCoroutine(TransitionRoutine(SceneManager.GetActiveScene().name, outDuration, inDuration));
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;

        StartCoroutine(TransitionRoutine("Mainmenu", durationOut, durationIn));
    }

    public void GoToMenu(float outDuration, float inDuration)
    {
        Time.timeScale = 1f;

        StartCoroutine(TransitionRoutine("Mainmenu", outDuration, inDuration));
    }

    public IEnumerator PlayAnimatePublic(float from, float to, float dur)
    {
        yield return StartCoroutine(Animate(from, to, dur));
    }

    IEnumerator TransitionRoutine(string sceneName, float outDur, float inDur)
    {
        Debug.Log("TransitionRoutine START");

        dissolveOverlay.gameObject.SetActive(true);

        // Dissolve OUT
        yield return StartCoroutine(Animate(0f, 1f, outDur));

        Debug.Log("Before Load Scene");

        // โหลด scene
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;

        yield return null;
        yield return null;

        // Dissolve IN
        yield return StartCoroutine(Animate(1f, 0f, inDur));

        dissolveOverlay.gameObject.SetActive(false);

        Debug.Log("Transition END");
    }

    IEnumerator Animate(float from, float to, float dur)
    {
        float elapsed = 0f;

        while (elapsed < dur)
        {
            // ใช้ unscaledDeltaTime กันค้างตอน Time.timeScale = 0
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);

            _matInstance.SetFloat(DissolveID, Mathf.Lerp(from, to, t));

            yield return null;
        }

        _matInstance.SetFloat(DissolveID, to);
    }
}