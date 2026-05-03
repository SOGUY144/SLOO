using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ควบคุมการเคลื่อนที่และการโจมตีของตัวละคร
public class FightingController : MonoBehaviour
{
    [Header("Player Movement")]
    public float movementSpeed = 1f; // ความเร็วในการเคลื่อนที่ของตัวละคร
    public float rotationSpeed = 10f; // ความเร็วในการหมุนตัวของตัวละคร
    private CharacterController characterController; // ใช้ควบคุมการชนของตัวละคร
    private Animator animator; // ควบคุมแอนิเมชันของตัวละคร
    public bool isStunned = false;

    [Header("Player Fight")]
    public float attackCooldown = 0.5f; // คูลดาวน์ของการโจมตี
    public float dodgeCooldown = 1.5f; // คูลดาวน์ของการหลบ
    public int[] attackDamages = { 5, 8, 12, 15 }; // ความเสียหายแต่ละท่า
    public KnockbackType[] attackKnockbackTypes = { KnockbackType.None, KnockbackType.None, KnockbackType.Pushback, KnockbackType.Knockdown };
    public float[] attackKnockbackPowers = { 0f, 0f, 3f, 6f };
    // รายชื่อแอนิเมชันของการโจมตีแต่ละแบบ (0 = Attack1, 1 = Attack2, ...)
    public string[] attackAnumations = {"Attack1Animation","Attack2Animation","Attack3Animation","Attack4Animation"};
    public float dodgeDistance = 2f; // ระยะที่ตัวละครจะพุ่งไปข้างหน้าตอน dodge
    public float attackRadius = 2.2f;
    public Transform[] opponents;
    private float lastAttackTime; // เวลาโจมตีล่าสุด
    private float lastDodgeTime = -Mathf.Infinity; // เวลา dodge ล่าสุด เริ่มต้นด้วย -Infinity เพื่อให้ dodge ครั้งแรกทำได้ทันที

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
     public int maxHitsToKnockdown = 3; // โดนตีกี่ทีล้ม
     public float comboResetTime = 1.5f; // ไม่โดนตีกี่วิถึงจะรีเซ็ตคอมโบ
     private int currentHitCount = 0;
     private float lastTimeHit;

    void Awake()
    {
        currentHealth = maxHealth;
        //healthBar.GiveFullHealth(currentHealth);
        characterController = GetComponent<CharacterController>(); // ดึงคอมโพเนนต์ CharacterController
        animator = GetComponent<Animator>(); // ดึงคอมโพเนนต์ Animator
    }

    void Update()
    {
        if (isStunned) return; // ล็อคการขยับและโจมตีตอนติดสตัน

        PerformMovement(); // เรียกฟังก์ชันเคลื่อนที่
        PerformDodgeFront(); // เรียกฟังก์ชันหลบ

        // ตรวจสอบการกดปุ่มตัวเลข 1 ถึง 4 เพื่อเลือกท่าโจมตี
        if (Input.GetKeyDown(KeyCode.H))
        {
            PerformAttack(0);
        }
        else if (Input.GetKeyDown(KeyCode.J))
        {
            PerformAttack(1);
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            PerformAttack(2);
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            PerformAttack(3);
        }
    }

    void PerformMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal"); // รับค่าการกดซ้าย-ขวา (A/D หรือ ลูกศร)
        float verticalInput = Input.GetAxis("Vertical"); // รับค่าการกดหน้า-หลัง (W/S หรือ ลูกศร)

        // เวกเตอร์การเคลื่อนไหว (หมายเหตุ: แกน Z กับ X ถูกสลับตามมุมกล้องของเกม)
        Vector3 movement = new Vector3(-verticalInput, 0f, horizontalInput);

        if (movement != Vector3.zero) // ถ้ามีการกดปุ่มเคลื่อนที่
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement); // หมุนไปยังทิศทางของ movement
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); // หมุนอย่างนุ่มนวล
            animator.SetBool("Walking", true); // สั่งเล่นแอนิเมชันเดิน
        }
        else
        {
            animator.SetBool("Walking", false); // หยุดแอนิเมชันเดิน
        }

        characterController.Move(movement * movementSpeed * Time.deltaTime); // เคลื่อนที่
    }

    // ฟังก์ชันโจมตี รับ index เพื่อเลือกแอนิเมชันโจมตีจาก array
    void PerformAttack(int attackIndex)
    {
        // เช็คว่าโจมตีได้หรือยัง (รอคูลดาวน์ให้ครบ)
        if (Time.time - lastAttackTime > attackCooldown)
        {
            animator.Play(attackAnumations[attackIndex]); // เล่นแอนิเมชันตาม index

            int damage = attackDamages[attackIndex];
            KnockbackType kbType = attackKnockbackTypes[attackIndex];
            float kbPower = attackKnockbackPowers[attackIndex];
            
            Debug.Log("Performed attack " + (attackIndex + 1) + " dealing " + damage + " damage");

            lastAttackTime = Time.time; // บันทึกเวลาโจมตีล่าสุด

            foreach(Transform opponent in opponents)
            {
                if(Vector3.Distance(transform.position,opponent.position) <= attackRadius)
                {
                    Vector3 kbDir = (opponent.position - transform.position).normalized;
                    kbDir.y = 0; // ป้องกันการลอยขึ้นฟ้า
                    opponent.GetComponent<OpponentAI>().StartCoroutine(opponent.GetComponent<OpponentAI>().PlayHitDamageAnimation(damage, kbType, kbDir, kbPower));
                }
            }
        }
        else
        {
            Debug.Log("Cannot perform attack yet. Cooldown time remaining."); // แจ้งว่ากำลังคูลดาวน์
        }
    }

    // ฟังก์ชันหลบไปด้านหน้า (Dodge)
    void PerformDodgeFront()
    {
        // ตรวจสอบว่า Player กด E และพ้นระยะคูลดาวน์หรือยัง
        if (Input.GetKeyDown(KeyCode.E) && Time.time - lastDodgeTime > dodgeCooldown)
        {
            animator.Play("DodgeFrontAnimation"); // เล่นแอนิเมชันหลบ

            Vector3 dodgeDirection = transform.forward * dodgeDistance; // คำนวณทิศทางหลบ (ไปข้างหน้า)
            characterController.Move(dodgeDirection); // สั่งหลบโดยการ Move ตัวละคร

            lastDodgeTime = Time.time; // บันทึกเวลาหลบล่าสุด
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Cannot dodge yet. Cooldown time remaining."); // แจ้งว่าหลบไม่ได้เพราะยังไม่พ้นคูลดาวน์
        }
    }

    public IEnumerator PlayHitDamageAnimation(int takeDamage, KnockbackType kbType = KnockbackType.None, Vector3 kbDir = default(Vector3), float kbPower = 0f)
    {
        yield return new WaitForSeconds(0.5f);

        isStunned = true; // ล็อคการกระทำ

        // --- ระบบ Combo Knockdown ---
        if (Time.time - lastTimeHit > comboResetTime)
        {
            currentHitCount = 0; // รีเซ็ตถ้ารอดไปได้นานพอ
        }
        
        lastTimeHit = Time.time;
        currentHitCount++; // โดนบวก 1 ฮิตเสมอ

        if (kbType == KnockbackType.Knockdown)
        {
            // โดนท่าเกรียน ล้มเลยแล้วรีเซ็ต
            currentHitCount = 0;
        }
        else if (currentHitCount >= maxHitsToKnockdown)
        {
            // โดนตีครบจำนวนฮิต บังคับล้ม!
            kbType = KnockbackType.Knockdown;
            if (kbPower < 5f) kbPower = 5f; // กำหนดแรงไถลมาตรฐานขั้นต่ำถ้าโดนคอมโบล้ม
            currentHitCount = 0; // รีเซ็ตคอมโบ
            Debug.Log("Combo Limit Reached! Forced Knockdown.");
        }
        // -----------------------------

        if(hitSounds != null && hitSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, hitSounds.Length);
            AudioSource.PlayClipAtPoint(hitSounds[randomIndex], transform.position);
        }

        currentHealth -= takeDamage;
        healthBar.SetHealth(currentHealth);

        if(currentHealth <= 0)
        {
            Die();
        }

        if (kbType == KnockbackType.Knockdown)
        {
            animator.Play("KnockdownAnimation");
            StartCoroutine(ApplyKnockbackRoutine(kbDir, kbPower, 2.5f));
        }
        else if (kbType == KnockbackType.Pushback)
        {
            animator.Play("PushbackAnimation");
            StartCoroutine(ApplyKnockbackRoutine(kbDir, kbPower, 1.0f));
        }
        else
        {
            animator.Play("HitDamageAnimation");
            yield return new WaitForSeconds(0.5f);
            isStunned = false;
        }
    }

    private IEnumerator ApplyKnockbackRoutine(Vector3 direction, float power, float stunDuration)
    {
        float timer = 0f;
        float moveDuration = 0.3f; // เวลาไถล

        while (timer < moveDuration)
        {
            characterController.Move(direction * power * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(stunDuration - moveDuration);
        isStunned = false;
    }

    void Die()
    {
        Debug.Log("Player died.");
    }

    public void Attack1Effect()
    {
        attack1Effect.Play();
    }
     public void Attack2Effect()
    {
        attack2Effect.Play();
    }
     public void Attack3Effect()
    {
        attack3Effect.Play();
    }
     public void Attack4Effect()
    {
        attack4Effect.Play();
    }
    
}
