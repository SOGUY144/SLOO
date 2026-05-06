using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightingController : MonoBehaviour
{
    [Header("Player Movement")]
    public float movementSpeed = 5f;
    public float rotationSpeed = 10f;
    private CharacterController characterController;
    private PlayerAnimator playerAnimator;
    public bool isStunned = false;
    public bool isInvincible = false;
    private bool isTakingDamage = false;

    [Header("Knockdown & Get Up Timing")]
    public float getUpAnimationDuration = 1.5f;
    public float knockdownStunTime = 0.3f;

    [Header("Player Fight")]
    public float attackCooldown = 1f;
    public float dodgeCooldown = 1.5f;
    public int[] attackDamages;
    public KnockbackType[] attackKnockbackTypes;
    public float[] attackKnockbackPowers;
    public string[] attackAnumations;
    public float dodgeDistance = 2f;
    public float attackRadius = 2.2f;
    public Transform[] opponents;
    private float lastAttackTime;
    private float lastDodgeTime = -Mathf.Infinity;

    [Header("Inventory & Pickup")]
    public Transform handTransform;
    public float pickupRange = 2.5f;
    private PickableItem heldItem;

    [Header("Effects and Sound")]
    public ParticleSystem attack1Effect;
    public ParticleSystem attack2Effect;
    public ParticleSystem attack3Effect;
    public ParticleSystem attack4Effect;
    public AudioClip[] hitSounds;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;

    [Header("Combo & Knockdown System")]
    public int maxHitsToKnockdown = 5;
    public float comboWindowTime = 1.5f;
    private int currentHitCount = 0;
    private float lastTimeHit;
    private const float hitReactionDuration = 0.45f;
    private const float knockbackMoveDuration = 0.3f;

    void Awake()
    {
        currentHealth = maxHealth;
        characterController = GetComponent<CharacterController>();
        playerAnimator = GetComponent<PlayerAnimator>();
    }

    void Update()
    {
        if (isStunned || isInvincible) return;

        PerformMovement();
        PerformDodgeFront();

        if (Input.GetKeyDown(KeyCode.H)) PerformAttack(0);
        else if (Input.GetKeyDown(KeyCode.J)) PerformAttack(1);
        else if (Input.GetKeyDown(KeyCode.K)) PerformAttack(2);
        else if (Input.GetKeyDown(KeyCode.L)) PerformAttack(3);

        if (Input.GetKeyDown(KeyCode.F)) TryPickupItem();
        if (Input.GetKeyDown(KeyCode.G)) DropItem();
    }

    public IEnumerator PlayHitDamageAnimation(int takeDamage, KnockbackType kbType = KnockbackType.None, Vector3 kbDir = default(Vector3), float kbPower = 0f)
    {
        if (isInvincible) yield break;
        if (isTakingDamage) yield break;

        isTakingDamage = true;

        if (Time.time - lastTimeHit > comboWindowTime) currentHitCount = 0;
        lastTimeHit = Time.time;
        currentHitCount++;

        currentHealth -= takeDamage;
        if (healthBar != null) healthBar.SetHealth(currentHealth);
        if (hitSounds != null && hitSounds.Length > 0)
            AudioSource.PlayClipAtPoint(hitSounds[Random.Range(0, hitSounds.Length)], transform.position);

        if (currentHealth <= 0)
        {
            isTakingDamage = false;
            Die();
            yield break;
        }

        if (currentHitCount >= maxHitsToKnockdown || kbType == KnockbackType.Knockdown)
        {
            isStunned = true;
            isInvincible = true;
            isTakingDamage = false;
            currentHitCount = 0;

            playerAnimator.PlayHit();
            yield return StartCoroutine(ApplyKnockbackRoutine(kbDir, Mathf.Max(kbPower, 5f), knockbackMoveDuration));
            yield return new WaitForSeconds(Mathf.Max(knockdownStunTime, hitReactionDuration));
            playerAnimator.PlayIdle();

            isInvincible = false;
            isStunned = false;
            yield break;
        }

        isStunned = true;

        if (kbType == KnockbackType.Pushback)
        {
            playerAnimator.PlayPushback();
            yield return StartCoroutine(ApplyKnockbackRoutine(kbDir, Mathf.Max(kbPower, 3f), knockbackMoveDuration));
            yield return new WaitForSeconds(0.15f);
            playerAnimator.PlayIdle();
        }
        else
        {
            playerAnimator.PlayHit();
            yield return new WaitForSeconds(hitReactionDuration);
        }

        isStunned = false;
        isTakingDamage = false;
    }

    void PerformMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(-verticalInput, 0f, horizontalInput);

        if (movement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            playerAnimator.SetWalking(true);
        }
        else playerAnimator.SetWalking(false);

        characterController.Move(movement * movementSpeed * Time.deltaTime);
    }

    void PerformAttack(int attackIndex)
    {
        if (Time.time - lastAttackTime > attackCooldown)
        {
            string animName = "";
            int damage = 5;
            KnockbackType kbType = KnockbackType.None;
            float kbPower = 0f;

            if (heldItem != null)
            {
                if (heldItem.weaponAttackAnimations != null && attackIndex < heldItem.weaponAttackAnimations.Length)
                    animName = heldItem.weaponAttackAnimations[attackIndex];
                if (string.IsNullOrEmpty(animName)) return;
                if (heldItem.weaponAttackDamages != null && attackIndex < heldItem.weaponAttackDamages.Length) damage = heldItem.weaponAttackDamages[attackIndex];
                if (heldItem.weaponKnockbackTypes != null && attackIndex < heldItem.weaponKnockbackTypes.Length) kbType = heldItem.weaponKnockbackTypes[attackIndex];
                if (heldItem.weaponKnockbackPowers != null && attackIndex < heldItem.weaponKnockbackPowers.Length) kbPower = heldItem.weaponKnockbackPowers[attackIndex];
            }
            else
            {
                if (attackAnumations != null && attackIndex < attackAnumations.Length) animName = attackAnumations[attackIndex];
                if (string.IsNullOrEmpty(animName)) return;
                if (attackDamages != null && attackIndex < attackDamages.Length) damage = attackDamages[attackIndex];
                if (attackKnockbackTypes != null && attackIndex < attackKnockbackTypes.Length) kbType = attackKnockbackTypes[attackIndex];
                if (attackKnockbackPowers != null && attackIndex < attackKnockbackPowers.Length) kbPower = attackKnockbackPowers[attackIndex];
            }

            playerAnimator.PlayAttack(animName);
            lastAttackTime = Time.time;

            foreach (Transform opponent in opponents)
            {
                if (opponent != null && Vector3.Distance(transform.position, opponent.position) <= attackRadius)
                {
                    Vector3 kbDir = (opponent.position - transform.position).normalized;
                    kbDir.y = 0;
                    var opponentAI = opponent.GetComponent<OpponentAI>();
                    if (opponentAI != null) opponentAI.StartCoroutine(opponentAI.PlayHitDamageAnimation(damage, kbType, kbDir, kbPower));
                }
            }
        }
    }

    void PerformDodgeFront()
    {
        if (Input.GetKeyDown(KeyCode.E) && Time.time - lastDodgeTime > dodgeCooldown)
        {
            playerAnimator.PlayDodge();
            Vector3 dodgeDirection = transform.forward * dodgeDistance;
            characterController.Move(dodgeDirection);
            lastDodgeTime = Time.time;
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
        playerAnimator.PlayDie();
        isStunned = true;
        isInvincible = true;
    }

    public void Attack1Effect() { if (attack1Effect != null) attack1Effect.Play(); }
    public void Attack2Effect() { if (attack2Effect != null) attack2Effect.Play(); }
    public void Attack3Effect() { if (attack3Effect != null) attack3Effect.Play(); }
    public void Attack4Effect() { if (attack4Effect != null) attack4Effect.Play(); }

    void TryPickupItem()
    {
        if (heldItem != null) return;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRange);
        foreach (var hitCollider in hitColliders)
        {
            PickableItem item = hitCollider.GetComponent<PickableItem>();
            if (item != null) { heldItem = item; item.OnPickedUp(handTransform); break; }
        }
    }

    void DropItem() { if (heldItem != null) { heldItem.OnDropped(); heldItem = null; } }
}
