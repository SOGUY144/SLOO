using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private GameObject[] characters; // ลาก Character ทั้ง 3 ใส่
    // Element 0 = Character1, Element 1 = Character2, Element 2 = Character3

    private void Start()
    {
        int selectedIndex = GameData.Instance.selectedCharacterIndex;

        // ปิดทุกตัวก่อน แล้วเปิดแค่ตัวที่เลือก
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null)
                characters[i].SetActive(i == selectedIndex);
        }
    }
}