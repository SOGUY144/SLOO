using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelector : MonoBehaviour
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

    [Header("Map Scene Names")]
    [SerializeField] private string[] mapSceneNames;

    [Header("Back")]
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

        // รีเซ็ตสถานะ transition ทุกครั้ง
        _isTransitioning = false;

        if (_outlines != null)
        {
            UpdateSelection();
        }
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
            {
                ol = characterItems[i].AddComponent<Outline>();
            }

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

        // ขวา
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (_currentIndex + 1 < characterItems.Length)
            {
                _currentIndex++;
            }
        }

        // ซ้าย
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (_currentIndex - 1 >= 0)
            {
                _currentIndex--;
            }
        }

        // เล่นเสียงตอนเลื่อน
        if (_currentIndex != oldIndex)
        {
            if (moveSound != null)
            {
                moveSound.Play();
            }
        }

        // Confirm
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (confirmSound != null)
            {
                confirmSound.Play();
            }

            ConfirmSelection();
        }

        // Back
        if (Input.GetKeyDown(KeyCode.B))
        {
            GameData.Instance.selectedCharacterIndex = 0;

            characterSelectionPanel.SetActive(false);
            mapSelectionPanel.SetActive(true);
        }

        if (_currentIndex != _previousIndex)
        {
            UpdateSelection();
        }
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < characterItems.Length; i++)
        {
            _outlines[i].enabled = (i == _currentIndex);
        }

        for (int i = 0; i < characterBackgrounds.Length; i++)
        {
            if (characterBackgrounds[i] != null)
            {
                characterBackgrounds[i].SetActive(i == _currentIndex);
            }
        }

        _previousIndex = _currentIndex;
    }

    private void AnimateItems()
    {
        for (int i = 0; i < characterItems.Length; i++)
        {
            Vector3 target =
                (i == _currentIndex)
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
        // กัน Time.timeScale ค้างจาก ResultManager
        Time.timeScale = 1f;

        GameData.Instance.selectedCharacterIndex = _currentIndex;

        int mapIndex = GameData.Instance.selectedMapIndex;

        if (mapSceneNames.Length > mapIndex &&
            !string.IsNullOrEmpty(mapSceneNames[mapIndex]))
        {
            if (SceneTransition.Instance == null)
            {
                Debug.LogWarning("ไม่พบ SceneTransition! ใช้ SceneManager แทน");

                SceneManager.LoadScene(mapSceneNames[mapIndex]);
                return;
            }

            _isTransitioning = true;

            SceneTransition.Instance.GoToScene(mapSceneNames[mapIndex]);
        }
        else
        {
            Debug.LogWarning(
                "ไม่พบ Scene สำหรับ Map Index: " +
                mapIndex +
                " กรุณาตรวจสอบ Map Scene Names ใน Inspector"
            );
        }
    }
}