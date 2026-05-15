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

        // ค้นหาว่าตัวไหนคือผู้เล่น และตัวไหนคือบอท
        FightingController activePlayer = null;
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != null)
            {
                bool isActive = (i == selectedIndex);
                characters[i].SetActive(isActive);
                
                // ถ้าเป็นตัวที่เลือก ให้เก็บค่าไว้ส่งให้ RoundManager
                if (isActive) activePlayer = characters[i].GetComponent<FightingController>();
                
                Debug.Log(characters[i].name + " : " + (isActive ? "เปิด ✅" : "ปิด ❌"));
            }
        }

        // เชื่อมต่อกับ RoundManager
        if (RoundManager.Instance != null && activePlayer != null)
        {
            // หาบอทในฉาก (บอทมักจะมี OpponentAI ติดอยู่)
            OpponentAI activeOpponent = FindObjectOfType<OpponentAI>();
            
            // ส่งข้อมูลให้ RoundManager ทันที!
            RoundManager.Instance.AssignCombatants(activePlayer, activeOpponent);
            Debug.Log("CharacterSpawner: ส่งข้อมูลผู้เล่นและบอทให้ RoundManager แล้ว!");
        }
    }
}