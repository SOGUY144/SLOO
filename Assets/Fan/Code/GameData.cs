using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance;

    public int selectedMapIndex = 0;
    public int selectedCharacterIndex = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}