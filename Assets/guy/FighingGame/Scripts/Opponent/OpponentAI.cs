using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentAI : MonoBehaviour
{
    [Header("Opponent Movement")]
    public float movementSpeed = 1f;
    public float rotationSpeed = 10f;
    public CharacterController characterController;
    public Animator animator;

    [Header("Knockdown & Get Up Timing")]
    public float getUpAnimationDuration = 1.5f; // เวลาที่ใช้เล่นท่าลุก
    public float knockdownStunTime = 0.3f;      // เวลานอนรอที่พื้นก่อนลุก

    [Header("Opponent Fight")]
    public float attackCooldown = 0.5f;
    public float dodgeCooldown = 1.5f;
    public int[] attackDamages = { 5, 8, 12, 15 };
    public KnockbackType[] attackKnockbackTypes = { KnockbackType.None, KnockbackType.None, KnockbackType.Pushback, KnockbackType.Knockdown };
    public float[] attackKnockbackPowers = { 0f, 0f, 3f, 6f };
    public string[] attackAnumations = {"Attack1Animation","Attack2Animation","Attack3Animation","Attack4Animation"};
    public float dodgeDistance = 2f;
    public int attackCount = 0;
    public int randomNumber;
    public float attackRadius = 2f;
    public FightingController[] fightingController;
    public Transform[] players;
    public bool isTakingDamage = false;    // ป้องกัน Coroutine ซ้อนกัน
    public bool isKnockedDown = false;      // กำลังล้มอยู่ (invincible)
    public bool isStunned = false;
    private float lastAttackTime;
    private float lastDodgeTime = -Mathf.Infinity;

    [Header("Effects and Sound")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public ParticleSystem attack1Effect;
    public ParticleSystem attack2Effect;
    public ParticleSystem attack3Effect;
    public ParticleSystem attack4Effect;
    public AudioClip[] attackSounds;
    public AudioClip[] hitSounds;
    private AudioSource audioSource;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;

    [Header("Combo & Knockdown System")]
    public int maxHitsToKnockdown = 3;
    public float comboResetTime = 1.5f;
    private int currentHitCount = 0;
    private float lastTimeHit;

    void Awake()
    {
        currentHealth = maxHealth;
        CreateRandomNuber();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (isStunned || isKnockedDown) return;

        for (int i = 0; i < fightingController.Length; i++)
        {
            // ✅ เช็ค null ก่อนใช้งานทุกครั้ง
            if (players[i] == null || fightingController[i] == null) continue;

            if (players[i].gameObject.activeSelf && Vector3.Distance(transform.position, players[i].position) <= attackRadius)
            {
                animator.SetBool("Walking", false);

                if (Time.time - lastAttackTime > attackCooldown)
                {
                    lastAttackTime = Time.time; // อัปเดต cooldown เสมอ ไม่ว่าจะ isTakingDamage หรือไม่

                    int randomAttackIndex = Random.Range(0, attackAnumations.Length);

                    if (!isTakingDamage)
                    {
                        PerformAttack(randomAttackIndex);

                        // โจมตี player เฉพาะตอนที่ไม่ได้โดนตีอยู่
                        if (!fightingController[i].isStunned && !fightingController[i].isInvincible)
                        {
                            int damage = (attackDamages != null && randomAttackIndex < attackDamages.Length) ? attackDamages[randomAttackIndex] : 5;
                            KnockbackType kbType = (attackKnockbackTypes != null && randomAttackIndex < attackKnockbackTypes.Length) ? attackKnockbackTypes[randomAttackIndex] : KnockbackType.None;
                            float kbPower = (attackKnockbackPowers != null && randomAttackIndex < attackKnockbackPowers.Length) ? attackKnockbackPowers[randomAttackIndex] : 0f;
                            Vector3 kbDir = (players[i].position - transform.position).normalized;
                            kbDir.y = 0;

                            fightingController[i].StartCoroutine(fightingController[i].PlayHitDamageAnimation(damage, kbType, kbDir, kbPower));
                        }
                    }
                }
            }
            else
            {
                if (players[i].gameObject.activeSelf)
                {
                    Vector3 direction = (players[i].position - transform.position).normalized;
                    characterController.Move(direction * movementSpeed * Time.deltaTime);

                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                    animator.SetBool("Walking", true);
                }
            }
        }
    }

    void PerformAttack(int attackIndex)
    {
        animator.Play(attackAnumations[attackIndex], 0, 0f);

        if (attackSounds != null && attackSounds.Length > 0)
        {
            int soundIndex = Mathf.Clamp(attackIndex, 0, attackSounds.Length - 1);
            if (audioSource != null) audioSource.PlayOneShot(attackSounds[soundIndex], sfxVolume);
        }

        int damage = (attackDamages != null && attackIndex < attackDamages.Length) ? attackDamages[attackIndex] : 5;
        Debug.Log("Performed attack " + (attackIndex + 1) + " dealing " + damage + " damage");
    }

    void PerformDodgeFront()
    {
        animator.Play("DodgeFrontAnimation");

        Vector3 dodgeDirection = -transform.forward * dodgeDistance;
        characterController.Move(dodgeDirection);

        lastDodgeTime = Time.time;
    }

    void CreateRandomNuber()
    {
        randomNumber = Random.Range(1, 5);
    }

    public IEnumerator PlayHitDamageAnimation(int takeDamage, KnockbackType kbType = KnockbackType.None, Vector3 kbDir = default(Vector3), float kbPower = 0f)
    {
        // ถ้ากำลังล้มอยู่ (invincible) ไม่รับ damage ใหม่เลย
        if (isKnockedDown) yield break;

        // ป้องกัน Coroutine โดนตีซ้อนกัน (แต่ยังนับ hit ได้)
        if (isTakingDamage) yield break;
        isTakingDamage = true;

        // --- ระบบ Combo Knockdown ---
        // ถ้าเวลาผ่านไปนานเกิน comboResetTime ให้ reset counter
        if (Time.time - lastTimeHit > comboResetTime)
            currentHitCount = 0;

        lastTimeHit = Time.time;
        currentHitCount++;

        Debug.Log($"[HitCount] {currentHitCount} / {maxHitsToKnockdown} | kbType: {kbType}");

        // ถ้า attack นี้เป็น Knockdown อยู่แล้ว ให้ reset counter
        if (kbType == KnockbackType.Knockdown)
        {
            currentHitCount = 0;
        }
        // ถ้า combo ครบกำหนด → บังคับ Knockdown
        else if (currentHitCount >= maxHitsToKnockdown)
        {
            kbType = KnockbackType.Knockdown;
            if (kbPower < 5f) kbPower = 5f;
            currentHitCount = 0;
            Debug.Log("[Combo Knockdown] Forced Knockdown after " + maxHitsToKnockdown + " hits!");
        }
        // ----------------------------

        // เล่นเสียง hit
        if (hitSounds != null && hitSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, hitSounds.Length);
            if (audioSource != null) audioSource.PlayOneShot(hitSounds[randomIndex], sfxVolume);
            else AudioSource.PlayClipAtPoint(hitSounds[randomIndex], transform.position);
        }

        // ลด HP
        currentHealth -= takeDamage;
        if (healthBar != null) healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0)
        {
            isTakingDamage = false;
            Die();
            yield break;
        }

        // --- เล่น Animation ตาม kbType ---
        if (kbType == KnockbackType.Knockdown)
        {
            // ⚠️ ไม่มี KnockdownAnimation ใน Animator → ใช้ HitDamageAnimation + stun นานขึ้นแทน
            isKnockedDown = true;
            isStunned = true;
            isTakingDamage = false;

            animator.Play("HitDamageAnimation", 0, 0f);
            yield return StartCoroutine(ApplyKnockbackRoutine(kbDir, kbPower, 0.3f));
            // นิ่งชะงัก (Stun) แป๊บนึง (ปรับได้ใน Inspector ผ่าน knockdownStunTime)
            yield return new WaitForSeconds(knockdownStunTime);

            // ข้ามท่า GetUp ไปเลย แล้วบังคับกลับท่ายืน Idle ทันที
            animator.CrossFade("IdleAnimation", 0.1f, 0, 0f);

            isKnockedDown = false;
            isStunned = false;
        }
        else if (kbType == KnockbackType.Pushback)
        {
            isStunned = true;
            animator.Play("PushbackAnimation", 0, 0f);
            yield return StartCoroutine(ApplyKnockbackRoutine(kbDir, kbPower, 0.3f));
            yield return new WaitForSeconds(0.5f);

            // บังคับกลับ IdleAnimation ทันทีหลัง Pushback
            animator.CrossFade("IdleAnimation", 0.1f, 0, 0f);

            isStunned = false;
            isTakingDamage = false;
        }
        else
        {
            // Hit ธรรมดา
            isStunned = true;
            animator.Play("HitDamageAnimation", 0, 0f);
            yield return new WaitForSeconds(0.45f);

            isStunned = false;
            isTakingDamage = false;
        }
    }

    private IEnumerator ApplyKnockbackRoutine(Vector3 direction, float power, float moveDuration)
    {
        float timer = 0f;

        while (timer < moveDuration)
        {
            characterController.Move(direction * power * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    void Die()
    {
        isKnockedDown = true;
        isStunned = true;
        animator.Play("HitDamageAnimation", 0, 0f);
        Debug.Log("Opponent died.");
    }

    public void Attack1Effect() { if (attack1Effect != null) attack1Effect.Play(); }
    public void Attack2Effect() { if (attack2Effect != null) attack2Effect.Play(); }
    public void Attack3Effect() { if (attack3Effect != null) attack3Effect.Play(); }
    public void Attack4Effect() { if (attack4Effect != null) attack4Effect.Play(); }
}
