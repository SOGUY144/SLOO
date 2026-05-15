using System.Collections;
using UnityEngine;

public class SceneIntro : MonoBehaviour
{
    [Tooltip("ความเร็วตอน dissolve เข้าตอนเริ่ม scene (วินาที)")]
    public float introDuration = 1.0f;

    IEnumerator Start()
    {
        if (SceneTransition.Instance == null) yield break;

        // ค้างที่ 1 ก่อน
        SceneTransition.Instance.dissolveOverlay.gameObject.SetActive(true);
        yield return StartCoroutine(
            SceneTransition.Instance.PlayAnimatePublic(1f, 0f, introDuration)
        );
        SceneTransition.Instance.dissolveOverlay.gameObject.SetActive(false);
    }
}