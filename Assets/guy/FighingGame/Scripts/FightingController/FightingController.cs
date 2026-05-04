using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FightingController : MonoBehaviour
{
    [Header("Player Movement")]
    public float movementSpeed = 5f; 
    public float rotationSpeed = 10f; 
    private CharacterController characterController; 
    private Animator animator; 
    public bool isStunned = false;
    private bool isInvincible = false; // ตัวแปรควบคุมสถานะอมตะ (Internal)

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

    void Awake()
    {
        currentHealth = maxHealth;
        characterController = GetComponent<CharacterController>(); 
        animator = GetComponent<Animator>(); 
    }

    void Update()
    {
        if (isStunned) return; 

        PerformMovement(); 
        PerformDodgeFront(); 

        if (Input.GetKeyDown(KeyCode.H)) PerformAttack(0);
        else if (Input.GetKeyDown(KeyCode.J)) PerformAttack(1);
        else if (Input.GetKeyDown(KeyCode.K)) PerformAttack(2);
        else if (Input.GetKeyDown(KeyCode.L)) PerformAttack(3);
        
        if (Input.GetKeyDown(KeyCode.F)) TryPickupItem();
        if (Input.GetKeyDown(KeyCode.G)) DropItem();
    }

    // --- ส่วนที่ปรับปรุง: ปลอดภัยและไม่กระทบส่วนอื่น ---
    public IEnumerator PlayHitDamageAnimation(int takeDamage, KnockbackType kbType = KnockbackType.None, Vector3 kbDir = default(Vector3), float kbPower = 0f)
    {
        // กฎเหล็ก: ถ้าอมตะอยู่ (ล้ม/ลุก) จะไม่รันโค้ดส่วนลดเลือดข้างล่างเลย
        if (isInvincible) yield break;

        // ระบบคอมโบ
        if (Time.time - lastTimeHit > comboWindowTime) currentHitCount = 0;
        lastTimeHit = Time.time; 
        currentHitCount++; 
        
        isStunned = true; // หยุดการควบคุมชั่วคราว

        // จัดการเลือดและเสียง
        currentHealth -= takeDamage;
        if(healthBar != null) healthBar.SetHealth(currentHealth);
        if(hitSounds != null && hitSounds.Length > 0)
        {
            AudioSource.PlayClipAtPoint(hitSounds[Random.Range(0, hitSounds.Length)], transform.position);
        }

        if (currentHealth <= 0)
        {
            Die();
            yield break;
        }

        // เช็กเงื่อนไขการล้ม
        if (currentHitCount >= maxHitsToKnockdown || kbType == KnockbackType.Knockdown)
        {
            isInvincible = true; // เปิดโหมดอมตะ
            currentHitCount = 0; 

            animator.Play("Falling_Down"); 
            StartCoroutine(ApplyKnockbackRoutine(kbDir, 5f, 0.3f));
            
            yield return new WaitForSeconds(1.5f); // นอนอยู่ (ปรับตามคลิปแอนิเมชัน)

            animator.Play("Getting_Up"); 
            yield return new WaitForSeconds(1.0f); // กำลังลุก (ปรับตามคลิปแอนิเมชัน)

            isInvincible = false; // ปิดโหมดอมตะ
        }
        else
        {
            animator.Play("HitDamageAnimation");
            yield return new WaitForSeconds(0.5f);
        }

        isStunned = false; // กลับมาควบคุมได้ปกติ
    }

    // --- ฟังก์ชันดั้งเดิม (ไม่มีการเปลี่ยนแปลงโครงสร้าง) ---
    void PerformMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal"); 
        float verticalInput = Input.GetAxis("Vertical"); 
        Vector3 movement = new Vector3(-verticalInput, 0f, horizontalInput);

        if (movement != Vector3.zero) 
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement); 
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); 
            animator.SetBool("Walking", true); 
        }
        else { animator.SetBool("Walking", false); }

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

            animator.Play(animName);
            lastAttackTime = Time.time; 

            foreach(Transform opponent in opponents)
            {
                if(opponent != null && Vector3.Distance(transform.position, opponent.position) <= attackRadius)
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
            animator.Play("DodgeFrontAnimation"); 
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
        Debug.Log("<color=red><b>PLAYER HAS DIED!</b></color>"); 
        animator.Play("Falling_Down"); 
        isStunned = true;
        isInvincible = true; 
    }

    public void Attack1Effect() { if(attack1Effect != null) attack1Effect.Play(); }
    public void Attack2Effect() { if(attack2Effect != null) attack2Effect.Play(); }
    public void Attack3Effect() { if(attack3Effect != null) attack3Effect.Play(); }
    public void Attack4Effect() { if(attack4Effect != null) attack4Effect.Play(); }
    
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