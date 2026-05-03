using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentAI : MonoBehaviour
{
    [Header("Opponent Movement")]
    public float movementSpeed = 1f; // ความเร็วในการเคลื่อนที่ของตัวละคร
    public float rotationSpeed = 10f; // ความเร็วในการหมุนตัวของตัวละคร
    public CharacterController characterController; // ใช้ควบคุมการชนของตัวละคร
    public Animator animator; // ควบคุมแอนิเมชันของตัวละคร

    [Header("Opponent Fight")]
    public float attackCooldown = 0.5f; // คูลดาวน์ของการโจมตี
    public float dodgeCooldown = 1.5f; // คูลดาวน์ของการหลบ
    public int[] attackDamages = { 5, 8, 12, 15 }; // ความเสียหายของการโจมตี
    public KnockbackType[] attackKnockbackTypes = { KnockbackType.None, KnockbackType.None, KnockbackType.Pushback, KnockbackType.Knockdown };
    public float[] attackKnockbackPowers = { 0f, 0f, 3f, 6f };
    public string[] attackAnumations = {"Attack1Animation","Attack2Animation","Attack3Animation","Attack4Animation"}; // รายชื่อแอนิเมชันโจมตี
    public float dodgeDistance = 2f; // ระยะทางที่หลบเมื่อ dodge
    public int attackCount = 0; // ตัวนับจำนวนการโจมตี
    public int randomNumber; // ตัวแปรเก็บเลขสุ่ม
    public float attackRadius = 2f; // รัศมีการตรวจสอบระยะโจมตี
    public FightingController[] fightingController; // อ้างอิงถึงระบบการต่อสู้ของผู้เล่น
    public Transform[] players; // ตำแหน่งของผู้เล่น
    public bool isTakingFammage; // ตรวจสอบว่าตัวละครกำลังโดนโจมตีหรือไม่
    public bool isStunned = false; // ตรวจสอบสถานะสตัน/กระเด็น
    private float lastAttackTime; // เวลาโจมตีล่าสุด
    private float lastDodgeTime = -Mathf.Infinity; // เวลา dodge ล่าสุด เริ่มที่ -Infinity เพื่อให้สามารถหลบได้ทันทีเมื่อเริ่ม

    [Header("Effects and Sound")]
    [Range(0f, 1f)] public float sfxVolume = 1f; // ความดังของเสียง
    public ParticleSystem attack1Effect; // เอฟเฟกต์สำหรับการโจมตีที่ 1
    public ParticleSystem attack2Effect; // เอฟเฟกต์สำหรับการโจมตีที่ 2
    public ParticleSystem attack3Effect; // เอฟเฟกต์สำหรับการโจมตีที่ 3
    public ParticleSystem attack4Effect; // เอฟเฟกต์สำหรับการโจมตีที่ 4
    
    public AudioClip[] attackSounds; // เสียงตอนโจมตี
    public AudioClip[] hitSounds; // เสียงตอนโดนตี
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
        //healthBar.GiveFullHealth(currentHealth);
        CreateRandomNuber(); // สร้างเลขสุ่มตอนเริ่มเกม

        // ติดตั้ง AudioSource สำหรับเล่นเสียง
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 1 = 3D Sound (เสียงเบาลงตามระยะห่าง)
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (isStunned) return; // ล็อคการขยับและโจมตีตอนติดสตัน

        //if(attackCount == randomNumber)
        //{
        //    attackCount = 0;
        //    createRandomNuber();
        //}

        // วนลูปตรวจสอบผู้เล่นแต่ละคน
        for(int i = 0; i < fightingController.Length; i++)
        {
            if(players[i].gameObject.activeSelf  && Vector3.Distance(transform.position,players[i].position) <= attackRadius)
            {
                animator.SetBool("Walking", false);

                if(Time.time - lastAttackTime > attackCooldown)
                {
                    int randomAttackIndex = Random.Range(0, attackAnumations.Length);

                    if(!isTakingFammage)
                    {
                        PerformAttack(randomAttackIndex);
                    }

                    int damage = attackDamages[randomAttackIndex];
                    KnockbackType kbType = attackKnockbackTypes[randomAttackIndex];
                    float kbPower = attackKnockbackPowers[randomAttackIndex];
                    Vector3 kbDir = (players[i].position - transform.position).normalized;
                    kbDir.y = 0; // ป้องกันลอยขึ้นฟ้า

                    fightingController[i].StartCoroutine(fightingController[i].PlayHitDamageAnimation(damage, kbType, kbDir, kbPower));
                }
            }
            else
            {
                if(players[i].gameObject.activeSelf) // ถ้าผู้เล่นคนนี้ยังเปิดใช้งานอยู่
                {
                    Vector3 direction = (players[i].position - transform.position).normalized; // คำนวณทิศทางไปยังผู้เล่น
                    characterController.Move(direction * movementSpeed * Time.deltaTime); // เคลื่อนที่ไปยังผู้เล่น

                    Quaternion targetRotation = Quaternion.LookRotation(direction); // หมุนไปยังผู้เล่น
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); // หมุนอย่าง Smooth

                    animator.SetBool("Walking", true); // เปิดแอนิเมชันเดิน
                }
            }
        }
    }

    // ฟังก์ชันโจมตี รับ index เพื่อเลือกแอนิเมชันโจมตีจาก array
    void PerformAttack(int attackIndex)
    {
        animator.Play(attackAnumations[attackIndex]); // เล่นแอนิเมชันตาม index ที่ส่งเข้ามา

        // --- เล่นเสียงโจมตี ---
        if (attackSounds != null && attackSounds.Length > 0)
        {
            int soundIndex = Mathf.Clamp(attackIndex, 0, attackSounds.Length - 1);
            if (audioSource != null) audioSource.PlayOneShot(attackSounds[soundIndex], sfxVolume);
        }

        int damage = attackDamages[attackIndex]; // ความเสียหายตามท่า
        Debug.Log("Performed attack " + (attackIndex + 1) + " dealing " + damage + " damage"); // แสดงใน Console

        lastAttackTime = Time.time; // บันทึกเวลาโจมตีล่าสุด
    }

    // ฟังก์ชันสำหรับทำการหลบไปข้างหน้า
    void PerformDodgeFront()
    {
        animator.Play("DodgeFrontAnimation"); // เล่นแอนิเมชันหลบ

        Vector3 dodgeDirection = -transform.forward * dodgeDistance; // คำนวณทิศทางการหลบ (ถอยหลังจากมุมมองของตัวเอง)
        characterController.Move(dodgeDirection); // เคลื่อนที่หลบ

        lastDodgeTime = Time.time; // บันทึกเวลาที่หลบล่าสุด
    }

    // ฟังก์ชันสุ่มเลข 1 ถึง 4 เพื่อใช้สุ่มแอนิเมชันหรือการกระทำอื่น ๆ
    void CreateRandomNuber()
    {
        randomNumber = Random.Range(1, 5); // สุ่มเลขระหว่าง 1 ถึง 4
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
            if (audioSource != null) audioSource.PlayOneShot(hitSounds[randomIndex], sfxVolume);
            else AudioSource.PlayClipAtPoint(hitSounds[randomIndex], transform.position);
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
        Debug.Log("Opponent died.");
    }


    // ฟังก์ชันแสดงเอฟเฟกต์การโจมตีที่ 1
    public void Attack1Effect()
    {
        attack1Effect.Play(); // เล่นเอฟเฟกต์
    }

    // ฟังก์ชันแสดงเอฟเฟกต์การโจมตีที่ 2
    public void Attack2Effect()
    {
        attack2Effect.Play();
    }

    // ฟังก์ชันแสดงเอฟเฟกต์การโจมตีที่ 3
    public void Attack3Effect()
    {
        attack3Effect.Play();
    }

    // ฟังก์ชันแสดงเอฟเฟกต์การโจมตีที่ 4
    public void Attack4Effect()
    {
        attack4Effect.Play();
    }
}
