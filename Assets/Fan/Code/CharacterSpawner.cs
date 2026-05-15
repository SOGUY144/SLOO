using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private GameObject[] characters;
    // Element 0 = Character1
    // Element 1 = Character2
    // Element 2 = Character3

    private void Start()
    {
        // เช็คว่า GameData มีอยู่ไหม
        if (GameData.Instance == null)
        {
            Debug.LogWarning("ไม่พบ GameData! ตรวจสอบว่ามี GameData ใน Selection Scene");
            return;
        }

        int selectedIndex = GameData.Instance.selectedCharacterIndex;
        Debug.Log("Character ที่เลือก Index: " + selectedIndex);

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null)
            {
                characters[i].SetActive(i == selectedIndex);
                Debug.Log(characters[i].name + " : " + (i == selectedIndex ? "เปิด ✅" : "ปิด ❌"));
            }
        }
    }
}