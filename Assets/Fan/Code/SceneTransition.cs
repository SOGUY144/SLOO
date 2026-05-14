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
    public float duration = 1.0f;

    private static readonly int DissolveID = Shader.PropertyToID("_Dissolve");
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

        _matInstance = new Material(dissolveMaterial);
        dissolveOverlay.material = _matInstance;
        _matInstance.SetFloat(DissolveID, 0f);
        dissolveOverlay.enabled = false;
    }

    // เรียกจาก CharacterSelector ตอนเลือกแมพ
    public void GoToScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    // Retry — โหลด scene ปัจจุบันใหม่
    public void RestartScene()
    {
        StartCoroutine(TransitionRoutine(SceneManager.GetActiveScene().name));
    }

    // กลับ MainMenu
    public void GoToMenu()
    {
        StartCoroutine(TransitionRoutine("Mainmenu"));
    }

    IEnumerator TransitionRoutine(string sceneName)
    {
        dissolveOverlay.enabled = true;

        // Dissolve OUT: 0 → 1 (หน้าจอมืด)
        yield return StartCoroutine(Animate(0f, 1f));

        // โหลด scene ใหม่
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;

        // รอ 2 frame ให้ scene ใหม่ init
        yield return null;
        yield return null;

        // Dissolve IN: 1 → 0 (scene ใหม่โผล่)
        yield return StartCoroutine(Animate(1f, 0f));

        dissolveOverlay.enabled = false;
    }

    IEnumerator Animate(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _matInstance.SetFloat(DissolveID, Mathf.Lerp(from, to, t));
            yield return null;
        }
        _matInstance.SetFloat(DissolveID, to);
    }
}