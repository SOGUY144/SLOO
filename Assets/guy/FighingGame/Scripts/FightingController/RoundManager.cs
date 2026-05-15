using System.Collections;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    [Header("Round Settings")]
    public int totalRounds = 3;
    public float roundTime = 60f;
    public float roundIntroTime = 1.35f;
    public float fightMessageTime = 0.8f;
    public float koFreezeTime = 0.25f;
    public float koDisplayTime = 1.65f;
    public float nextRoundDelay = 0.85f;
    public float matchEndPanelDelay = 2.4f;

    [Header("References")]
    public FightingController player;
    public OpponentAI opponent;

    [Header("Round Start Positions")]
    public Transform playerStartPoint;
    public Transform opponentStartPoint;

    [Header("Audio")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    private AudioSource bgmSource;

    public int currentRound { get; private set; } = 1;
    public float timeRemaining { get; private set; }
    public int playerRoundWins { get; private set; } = 0;
    public int opponentRoundWins { get; private set; } = 0;
    public bool isRoundActive { get; private set; } = false;

    public System.Action<float> OnTimerUpdate;
    public System.Action<int, int> OnRoundWinsUpdate;
    public System.Action<string> OnMessageShow;
    public System.Action OnRoundStart;
    public System.Action OnRoundEnd;
    public System.Action<bool> OnGameEnd;

    private Vector3 playerInitialPosition;
    private Quaternion playerInitialRotation;
    private Vector3 opponentInitialPosition;
    private Quaternion opponentInitialRotation;
    private bool isEndingRound;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (backgroundMusic != null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.clip = backgroundMusic;
            bgmSource.volume = bgmVolume;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.Play();
        }

        CacheInitialPositions();
        StartCoroutine(StartRoundSequence());
    }

    void Update()
    {
        if (!isRoundActive || isEndingRound) return;

        timeRemaining -= Time.deltaTime;
        OnTimerUpdate?.Invoke(timeRemaining);

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            StartCoroutine(EndRound_TimeUp());
        }
    }

    public void OnPlayerDied()
    {
        if (!isRoundActive || isEndingRound) return;
        StartCoroutine(EndRound_KO(playerWon: false));
    }

    public void OnOpponentDied()
    {
        if (!isRoundActive || isEndingRound) return;
        StartCoroutine(EndRound_KO(playerWon: true));
    }

    private IEnumerator StartRoundSequence()
    {
        isEndingRound = false;
        isRoundActive = false;
        SetCombatantsLocked(true);
        ResetCombatants();

        OnTimerUpdate?.Invoke(roundTime);
        OnMessageShow?.Invoke("ROUND " + currentRound);
        yield return new WaitForSeconds(roundIntroTime);

        OnMessageShow?.Invoke("FIGHT!");
        yield return new WaitForSeconds(fightMessageTime);

        OnMessageShow?.Invoke("");
        timeRemaining = roundTime;
        isRoundActive = true;
        SetCombatantsLocked(false);
        OnRoundStart?.Invoke();
    }

    private IEnumerator EndRound_KO(bool playerWon)
    {
        isEndingRound = true;
        isRoundActive = false;
        SetCombatantsLocked(true);
        OnRoundEnd?.Invoke();

        if (koFreezeTime > 0f)
            yield return new WaitForSeconds(koFreezeTime);

        OnMessageShow?.Invoke("K.O.!");
        yield return new WaitForSeconds(koDisplayTime);

        yield return StartCoroutine(ApplyRoundResult(playerWon));
    }

    private IEnumerator EndRound_TimeUp()
    {
        isEndingRound = true;
        isRoundActive = false;
        SetCombatantsLocked(true);
        OnRoundEnd?.Invoke();

        bool playerWon = (player != null && opponent != null) &&
                         (player.currentHealth >= opponent.currentHealth);

        OnMessageShow?.Invoke("TIME UP!");
        yield return new WaitForSeconds(koDisplayTime);

        yield return StartCoroutine(ApplyRoundResult(playerWon));
    }

    private IEnumerator ApplyRoundResult(bool playerWon)
    {
        if (playerWon) playerRoundWins++;
        else opponentRoundWins++;

        OnRoundWinsUpdate?.Invoke(playerRoundWins, opponentRoundWins);

        int winsNeeded = Mathf.CeilToInt(totalRounds / 2f);
        if (playerRoundWins >= winsNeeded)
        {
            if (bgmSource != null) bgmSource.Stop();
            OnMessageShow?.Invoke("YOU WIN!");
            yield return new WaitForSeconds(matchEndPanelDelay);
            OnGameEnd?.Invoke(true);
            yield break;
        }

        if (opponentRoundWins >= winsNeeded)
        {
            if (bgmSource != null) bgmSource.Stop();
            OnMessageShow?.Invoke("YOU LOSE...");
            yield return new WaitForSeconds(matchEndPanelDelay);
            OnGameEnd?.Invoke(false);
            yield break;
        }

        currentRound++;
        yield return new WaitForSeconds(nextRoundDelay);
        StartCoroutine(StartRoundSequence());
    }

    private void ResetCombatants()
    {
        if (player != null)
        {
            ResetTransform(player.transform, playerStartPoint, playerInitialPosition, playerInitialRotation);
            player.currentHealth = player.maxHealth;
            player.isStunned = true;
            player.isInvincible = false;
            if (player.healthBar != null) player.healthBar.SetHealth(player.currentHealth);
            if (HUDController.Instance != null) HUDController.Instance.SetPlayerHP(player.currentHealth, player.maxHealth);
        }

        if (opponent != null)
        {
            ResetTransform(opponent.transform, opponentStartPoint, opponentInitialPosition, opponentInitialRotation);
            opponent.currentHealth = opponent.maxHealth;
            opponent.isStunned = true;
            opponent.isKnockedDown = false;
            opponent.isTakingDamage = false;
            if (opponent.healthBar != null) opponent.healthBar.SetHealth(opponent.currentHealth);
            if (HUDController.Instance != null) HUDController.Instance.SetOpponentHP(opponent.currentHealth, opponent.maxHealth);
        }
    }

    private void SetCombatantsLocked(bool locked)
    {
        if (player != null)
        {
            player.isStunned = locked;
            player.isInvincible = locked;
        }

        if (opponent != null)
        {
            opponent.isStunned = locked;
            opponent.isKnockedDown = locked;
        }
    }

    public void CacheInitialPositions()
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

    public void AssignCombatants(FightingController p, OpponentAI o)
    {
        player = p;
        opponent = o;
        SetCombatantsLocked(true);
        CacheInitialPositions();
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