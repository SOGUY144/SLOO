using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MapSelector : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int columns = 3;

    [Header("Map Items")]
    [SerializeField] private GameObject[] mapItems;

    [Header("Background")]
    [SerializeField] private GameObject[] mapBackgrounds;

    [Header("Selection Border")]
    [SerializeField] private Color borderColor = Color.red;
    [SerializeField] private float borderThickness = 3f;

    [Header("Scale Settings")]
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float scaleSpeed = 8f;

    [Header("Panels")]
    [SerializeField] private GameObject mapSelectionPanel;
    [SerializeField] private GameObject characterSelectionPanel;

    [Header("Back")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // ชื่อ Scene MainMenu

    private int _currentIndex = 0;
    private int _previousIndex = -1;
    private Vector3[] _originalScales;
    private Outline[] _outlines;

    private void Start()
    {
        _originalScales = new Vector3[mapItems.Length];
        _outlines = new Outline[mapItems.Length];

        for (int i = 0; i < mapItems.Length; i++)
        {
            _originalScales[i] = mapItems[i].transform.localScale;

            Outline ol = mapItems[i].GetComponent<Outline>();
            if (ol == null)
                ol = mapItems[i].AddComponent<Outline>();

            ol.effectColor = borderColor;
            ol.effectDistance = new Vector2(borderThickness, -borderThickness);
            ol.enabled = false;

            _outlines[i] = ol;
        }

        UpdateSelection();
    }

    private void Update()
    {
        HandleInput();
        AnimateItems();
    }

    private void HandleInput()
    {
        int col = _currentIndex % columns;

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (col + 1 < columns && _currentIndex + 1 < mapItems.Length)
                _currentIndex++;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (col - 1 >= 0)
                _currentIndex--;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            int next = _currentIndex + columns;
            if (next < mapItems.Length)
                _currentIndex = next;
        }
        else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            int prev = _currentIndex - columns;
            if (prev >= 0)
                _currentIndex = prev;
        }

        // Confirm
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            ConfirmSelection();

        // Back → ไป MainMenu Scene + Reset ค่า
        if (Input.GetKeyDown(KeyCode.B))
        {
            GameData.Instance.selectedMapIndex = 0;
            GameData.Instance.selectedCharacterIndex = 0;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        if (_currentIndex != _previousIndex)
            UpdateSelection();
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < mapItems.Length; i++)
            _outlines[i].enabled = (i == _currentIndex);

        for (int i = 0; i < mapBackgrounds.Length; i++)
        {
            if (mapBackgrounds[i] != null)
                mapBackgrounds[i].SetActive(i == _currentIndex);
        }

        _previousIndex = _currentIndex;
    }

    private void AnimateItems()
    {
        for (int i = 0; i < mapItems.Length; i++)
        {
            Vector3 target = (i == _currentIndex)
                ? _originalScales[i] * selectedScale
                : _originalScales[i];

            mapItems[i].transform.localScale = Vector3.Lerp(
                mapItems[i].transform.localScale,
                target,
                Time.deltaTime * scaleSpeed
            );
        }
    }

    private void ConfirmSelection()
    {
        GameData.Instance.selectedMapIndex = _currentIndex;
        mapSelectionPanel.SetActive(false);
        characterSelectionPanel.SetActive(true);
    }
}