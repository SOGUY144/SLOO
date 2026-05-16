using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelecHard : MonoBehaviour
{
    [Header("Character Items")]
    [SerializeField] private GameObject[] characterItems;

    [Header("Character Backgrounds")]
    [SerializeField] private GameObject[] characterBackgrounds;

    [Header("Selection Border")]
    [SerializeField] private Color borderColor = Color.yellow;
    [SerializeField] private float borderThickness = 3f;

    [Header("Scale Settings")]
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float scaleSpeed = 8f;

    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "Map1"; // ปรับได้เองใน Inspector

    [Header("Back Settings")]
    [SerializeField] private string backSceneName = "Mainmenu"; // ปรับได้เองใน Inspector
    [SerializeField] private GameObject mapSelectionPanel;
    [SerializeField] private GameObject characterSelectionPanel;

    [Header("Sound")]
    [SerializeField] private AudioSource moveSound;
    [SerializeField] private AudioSource confirmSound;

    private int _currentIndex = 0;
    private int _previousIndex = -1;
    private Vector3[] _originalScales;
    private Outline[] _outlines;
    private bool _isTransitioning = false;

    private void OnEnable()
    {
        _currentIndex = 0;
        _previousIndex = -1;
        _isTransitioning = false;

        if (_outlines != null)
            UpdateSelection();
    }

    private void Start()
    {
        _originalScales = new Vector3[characterItems.Length];
        _outlines = new Outline[characterItems.Length];

        for (int i = 0; i < characterItems.Length; i++)
        {
            _originalScales[i] = characterItems[i].transform.localScale;

            Outline ol = characterItems[i].GetComponent<Outline>();
            if (ol == null)
                ol = characterItems[i].AddComponent<Outline>();

            ol.effectColor = borderColor;
            ol.effectDistance = new Vector2(borderThickness, -borderThickness);
            ol.enabled = false;
            _outlines[i] = ol;
        }

        UpdateSelection();
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;
        if (_isTransitioning) return;

        HandleInput();
        AnimateItems();
    }

    private void HandleInput()
    {
        int oldIndex = _currentIndex;

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (_currentIndex + 1 < characterItems.Length)
                _currentIndex++;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (_currentIndex - 1 >= 0)
                _currentIndex--;
        }

        if (_currentIndex != oldIndex)
        {
            if (moveSound != null)
                moveSound.Play();
        }

        // Confirm → ไปแมพที่กำหนด
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (confirmSound != null)
                confirmSound.Play();

            ConfirmSelection();
        }

        // Back → กลับ Mainmenu
        if (Input.GetKeyDown(KeyCode.B))
        {
            GoBack();
        }

        if (_currentIndex != _previousIndex)
            UpdateSelection();
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < characterItems.Length; i++)
            _outlines[i].enabled = (i == _currentIndex);

        for (int i = 0; i < characterBackgrounds.Length; i++)
        {
            if (characterBackgrounds[i] != null)
                characterBackgrounds[i].SetActive(i == _currentIndex);
        }

        _previousIndex = _currentIndex;
    }

    private void AnimateItems()
    {
        for (int i = 0; i < characterItems.Length; i++)
        {
            Vector3 target = (i == _currentIndex)
                ? _originalScales[i] * selectedScale
                : _originalScales[i];

            characterItems[i].transform.localScale = Vector3.Lerp(
                characterItems[i].transform.localScale,
                target,
                Time.deltaTime * scaleSpeed
            );
        }
    }

    private void ConfirmSelection()
    {
        Time.timeScale = 1f;
        GameData.Instance.selectedCharacterIndex = _currentIndex;

        _isTransitioning = true;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.GoToScene(targetSceneName);
        else
            SceneManager.LoadScene(targetSceneName);
    }

    private void GoBack()
    {
        _isTransitioning = true;

        if (SceneTransition.Instance != null)
            SceneTransition.Instance.GoToScene(backSceneName);
        else
            SceneManager.LoadScene(backSceneName);
    }
}