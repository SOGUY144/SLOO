using System.Collections;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    // Singleton ให้เรียกใช้จากที่ไหนก็ได้
    public static RoundManager Instance;

    [Header("Round Settings")]
    public int totalRounds = 3;         // Best of 3
    public float roundTime = 60f;       // วินาทีต่อ Round
    public float messageDisplayTime = 2f; // นานแค่ไหนที่ข้อความ FIGHT! / KO! จะโชว์

    [Header("References")]
    public FightingController player;
    public OpponentAI opponent;

    [Header("Round Start Positions")]
    public Transform playerStartPoint;
    public Transform opponentStartPoint;

    // State
    public int currentRound { get; private set; } = 1;
    public float timeRemaining { get; private set; }
    public int playerRoundWins { get; private set; } = 0;
    public int opponentRoundWins { get; private set; } = 0;
    public bool isRoundActive { get; private set; } = false;

    // Events ที่ HUDController จะฟัง
    public System.Action<float> OnTimerUpdate;
    public System.Action<int, int> OnRoundWinsUpdate;      // (playerWins, opponentWins)
    public System.Action<string> OnMessageShow;            // ข้อความกลางจอ
    public System.Action OnRoundStart;
    public System.Action OnRoundEnd;
    public System.Action<bool> OnGameEnd;                  // true = player ชนะ

    private Vector3 playerInitialPosition;
    private Quaternion playerInitialRotation;
    private Vector3 opponentInitialPosition;
    private Quaternion opponentInitialRotation;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        CacheInitialPositions();
        StartCoroutine(StartRoundSequence());
    }

    void Update()
    {
        if (!isRoundActive) return;

        timeRemaining -= Time.deltaTime;
        OnTimerUpdate?.Invoke(timeRemaining);

        // หมดเวลา
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            StartCoroutine(EndRound_TimeUp());
        }
    }

    // เรียกเมื่อ Player ตาย (จาก FightingController)
    public void OnPlayerDied()
    {
        if (!isRoundActive) return;
        StartCoroutine(EndRound_KO(playerWon: false));
    }

    // เรียกเมื่อ Opponent ตาย (จาก OpponentAI)
    public void OnOpponentDied()
    {
        if (!isRoundActive) return;
        StartCoroutine(EndRound_KO(playerWon: true));
    }

    // --- ROUND FLOW ---
    private IEnumerator StartRoundSequence()
    {
        isRoundActive = false;

        // รีเซ็ต HP ทั้งคู่
        ResetCombatants();

        // แสดงข้อความ "ROUND X"
        OnMessageShow?.Invoke("ROUND " + currentRound);
        yield return new WaitForSeconds(messageDisplayTime);

        // แสดง "FIGHT!"
        OnMessageShow?.Invoke("FIGHT!");
        yield return new WaitForSeconds(1f);

        OnMessageShow?.Invoke("");
        timeRemaining = roundTime;
        isRoundActive = true;
        OnRoundStart?.Invoke();
    }

    private IEnumerator EndRound_KO(bool playerWon)
    {
        isRoundActive = false;
        OnRoundEnd?.Invoke();

        // แสดง KO!
        OnMessageShow?.Invoke("K.O.!");
        yield return new WaitForSeconds(messageDisplayTime);

        // บันทึกแต้ม
        if (playerWon) playerRoundWins++;
        else opponentRoundWins++;

        OnRoundWinsUpdate?.Invoke(playerRoundWins, opponentRoundWins);

        // เช็คว่าใครชนะเกม
        int winsNeeded = Mathf.CeilToInt(totalRounds / 2f);
        if (playerRoundWins >= winsNeeded)
        {
            OnMessageShow?.Invoke("YOU WIN!");
            OnGameEnd?.Invoke(true);
            yield break;
        }
        else if (opponentRoundWins >= winsNeeded)
        {
            OnMessageShow?.Invoke("YOU LOSE...");
            OnGameEnd?.Invoke(false);
            yield break;
        }

        // ยังไม่จบเกม ขึ้น Round ต่อไป
        currentRound++;
        yield return new WaitForSeconds(1f);
        StartCoroutine(StartRoundSequence());
    }

    private IEnumerator EndRound_TimeUp()
    {
        isRoundActive = false;
        OnRoundEnd?.Invoke();

        OnMessageShow?.Invoke("TIME UP!");
        yield return new WaitForSeconds(messageDisplayTime);

        // ใครเลือดเหลือมากกว่า = ชนะ Round นี้
        bool playerWon = (player != null && opponent != null) &&
                         (player.currentHealth >= opponent.currentHealth);

        if (playerWon) playerRoundWins++;
        else opponentRoundWins++;

        OnRoundWinsUpdate?.Invoke(playerRoundWins, opponentRoundWins);

        int winsNeeded = Mathf.CeilToInt(totalRounds / 2f);
        if (playerRoundWins >= winsNeeded)
        {
            OnMessageShow?.Invoke("YOU WIN!");
            OnGameEnd?.Invoke(true);
            yield break;
        }
        else if (opponentRoundWins >= winsNeeded)
        {
            OnMessageShow?.Invoke("YOU LOSE...");
            OnGameEnd?.Invoke(false);
            yield break;
        }

        currentRound++;
        yield return new WaitForSeconds(1f);
        StartCoroutine(StartRoundSequence());
    }

    private void ResetCombatants()
    {
        // รีเซ็ต Player
        if (player != null)
        {
            ResetTransform(player.transform, playerStartPoint, playerInitialPosition, playerInitialRotation);
            player.currentHealth = player.maxHealth;
            player.isStunned = false;
            player.isInvincible = false;
            if (player.healthBar != null) player.healthBar.SetHealth(player.currentHealth);
            if (HUDController.Instance != null) HUDController.Instance.SetPlayerHP(player.currentHealth, player.maxHealth);
        }

        // รีเซ็ต Opponent
        if (opponent != null)
        {
            ResetTransform(opponent.transform, opponentStartPoint, opponentInitialPosition, opponentInitialRotation);
            opponent.currentHealth = opponent.maxHealth;
            opponent.isStunned = false;
            opponent.isKnockedDown = false;
            opponent.isTakingDamage = false;
            if (opponent.healthBar != null) opponent.healthBar.SetHealth(opponent.currentHealth);
            if (HUDController.Instance != null) HUDController.Instance.SetOpponentHP(opponent.currentHealth, opponent.maxHealth);
        }
    }

    private void CacheInitialPositions()
    {
        if (player != null)
        {
            playerInitialPosition = player.transform.position;
            playerInitialRotation = player.transform.rotation;
        }

        if (opponent != null)
        {
            opponentInitialPosition = opponent.transform.position;
            opponentInitialRotation = opponent.transform.rotation;
        }
    }

    private void ResetTransform(Transform target, Transform startPoint, Vector3 fallbackPosition, Quaternion fallbackRotation)
    {
        CharacterController controller = target.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        if (startPoint != null)
            target.SetPositionAndRotation(startPoint.position, startPoint.rotation);
        else
            target.SetPositionAndRotation(fallbackPosition, fallbackRotation);

        if (controller != null) controller.enabled = true;
    }
}
