using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    [SerializeField] private Vector3 normalScale = Vector3.one;
    [SerializeField] private Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);

    [Header("Speed Settings")]
    [SerializeField] private float hoverInSpeed = 8f;   // ความเร็วตอน hover เข้า
    [SerializeField] private float hoverOutSpeed = 5f;  // ความเร็วตอน hover ออก

    private Coroutine _currentCoroutine;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        normalScale = _rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartHover(hoverScale, hoverInSpeed);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartHover(normalScale, hoverOutSpeed);
    }

    private void StartHover(Vector3 targetScale, float speed)
    {
        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        _currentCoroutine = StartCoroutine(ScaleTo(targetScale, speed));
    }

    private IEnumerator ScaleTo(Vector3 targetScale, float speed)
    {
        while (Vector3.Distance(_rectTransform.localScale, targetScale) > 0.001f)
        {
            _rectTransform.localScale = Vector3.Lerp(
                _rectTransform.localScale,
                targetScale,
                Time.deltaTime * speed
            );
            yield return null;
        }

        _rectTransform.localScale = targetScale;
    }
}