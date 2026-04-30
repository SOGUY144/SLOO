using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ควบคุมการเคลื่อนที่และการโจมตีของตัวละคร
public class FightingController : MonoBehaviour
{
    [Header("Player Movement")]
    public float movementSpeed = 5f; // ความเร็วในการเคลื่อนที่ของตัวละคร
    public float rotationSpeed = 10f; // ความเร็วในการหมุนตัวของตัวละคร
    public float gravity = 9.81f; // แรงโน้มถ่วง
    private float verticalVelocity; // ความเร็วในแนวตั้ง
    private CharacterController characterController; // ใช้ควบคุมการชนของตัวละคร
    private Animator animator; // ควบคุมแอนิเมชันของตัวละคร

    [Header("Player Fight")]
    public float attackCooldown = 0.5f; // คูลดาวน์ของการโจมตี
    public float dodgeCooldown = 1.5f; // คูลดาวน์ของการหลบ
    public int attackDamages = 5; // ความเสียหายของการโจมตี
    // รายชื่อแอนิเมชันของการโจมตีแต่ละแบบ (0 = Attack1, 1 = Attack2, ...)
    public string[] attackAnumations = {"Attack1Animation","Attack2Animation","Attack3Animation","Attack4Animation"};
    public float dodgeDistance = 2f; // ระยะที่ตัวละครจะพุ่งไปข้างหน้าตอน dodge
    public float attackRadius = 2.2f;
    public Transform[] opponents;
    private float lastAttackTime; // เวลาโจมตีล่าสุด
    private float lastDodgeTime = -Mathf.Infinity; // เวลา dodge ล่าสุด เริ่มต้นด้วย -Infinity เพื่อให้ dodge ครั้งแรกทำได้ทันที

    [Header("Inventory & Pickup")]
    public Transform handTransform; // จุดที่อาวุธจะไปติด (เช่น มือขวา)
    public float pickupRange = 2.5f; // ระยะที่สามารถหยิบของได้
    private PickableItem heldItem; // เก็บข้อมูลอาวุธที่ถืออยู่

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



    void Awake()
    {
        currentHealth = maxHealth;
        //healthBar.GiveFullHealth(currentHealth);
        characterController = GetComponent<CharacterController>(); // ดึงคอมโพเนนต์ CharacterController
        animator = GetComponent<Animator>(); // ดึงคอมโพเนนต์ Animator
    }

    void Update()
    {
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

        // ตรวจสอบการกดปุ่ม F เพื่อหยิบสิ่งของ
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryPickupItem();
        }

        // ตรวจสอบการกดปุ่ม G เพื่อทิ้งสิ่งของ
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropItem();
        }
    }

    void PerformMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal"); // รับค่าแบบดิบ (หยุดกึกทันที)
        float verticalInput = Input.GetAxisRaw("Vertical"); // รับค่าแบบดิบ (หยุดกึกทันที)

        // เวกเตอร์การเคลื่อนไหว (หมายเหตุ: แกน Z กับ X ถูกสลับตามมุมกล้องของเกมคุณ)
        Vector3 movement = new Vector3(-verticalInput, 0f, horizontalInput);

        if (movement.magnitude > 0.1f) // ถ้ามีการกดปุ่มเคลื่อนที่
        {
            movement.Normalize(); // Normalize เพื่อให้ความเร็วคงที่ทุกทิศทาง
            
            Quaternion targetRotation = Quaternion.LookRotation(movement); // หมุนไปยังทิศทางของ movement
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); // หมุนอย่างนุ่มนวล
            animator.SetBool("Walking", true); // สั่งเล่นแอนิเมชันเดิน
        }
        else
        {
            animator.SetBool("Walking", false); // หยุดแอนิเมชันเดิน
            movement = Vector3.zero;
        }

        // ระบบแรงโน้มถ่วง (Gravity)
        if (characterController.isGrounded)
        {
            verticalVelocity = -0.5f; // แรงกดพื้นเล็กน้อย
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime; // ตกลงตามแรงโน้มถ่วง
        }

        Vector3 moveVector = movement * movementSpeed;
        moveVector.y = verticalVelocity;

        //Debug.Log("Movement: " + moveVector); // เอาไว้เช็คใน Console ว่ามีแรงส่งไปที่ CharacterController ไหม

        characterController.Move(moveVector * Time.deltaTime); // เคลื่อนที่
    }

    // ฟังก์ชันโจมตี รับ index เพื่อเลือกแอนิเมชันโจมตีจาก array
    void PerformAttack(int attackIndex)
    {
        // เช็คว่าโจมตีได้หรือยัง (รอคูลดาวน์ให้ครบ)
        if (Time.time - lastAttackTime > attackCooldown)
        {
            // ถ้าถือของอยู่ ให้ใช้แอนิเมชันของอาวุธ ถ้าไม่ถือให้ใช้ท่าปกติ
            string animationName = (heldItem != null) ? heldItem.weaponAttackAnimations[attackIndex] : attackAnumations[attackIndex];
            animator.Play(animationName);

            int damage = attackDamages + (heldItem != null ? heldItem.additionalDamage : 0);
            Debug.Log("Performed attack " + animationName + " dealing " + damage + " damage");

            lastAttackTime = Time.time;

            foreach(Transform opponent in opponents)
            {
                if(Vector3.Distance(transform.position,opponent.position) <= attackRadius)
                {
                    opponent.GetComponent<OpponentAI>().StartCoroutine(opponent.GetComponent<OpponentAI>().PlayHitDamageAnimation(damage));
                }
            }
        }
        else
        {
            Debug.Log("Cannot perform attack yet. Cooldown time remaining.");
        }
    }

    // ฟังก์ชันหยิบของ
    void TryPickupItem()
    {
        if (heldItem != null) return; // ถ้าถืออยู่แล้ว ไม่ให้หยิบซ้ำ

        // ค้นหาวัตถุรอบๆ ตัว
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRange);
        foreach (var hitCollider in hitColliders)
        {
            PickableItem item = hitCollider.GetComponent<PickableItem>();
            if (item != null)
            {
                heldItem = item;
                item.OnPickedUp(handTransform); // ให้ของมาติดที่มือ
                Debug.Log("Picked up: " + item.itemName);
                break;
            }
        }
    }

    // ฟังก์ชันทิ้งของ
    void DropItem()
    {
        if (heldItem != null)
        {
            Debug.Log("Dropped: " + heldItem.itemName);
            heldItem.OnDropped(); // คืนค่าฟิสิกส์ให้วัตถุ
            heldItem = null; // เคลียร์สถานะว่าไม่ได้ถือของแล้ว
        }
    }

    // ฟังก์ชันหลบไปด้านหน้า (Dodge)
    void PerformDodgeFront()
    {
        // ตรวจสอบว่า Player กด Q และพ้นระยะคูลดาวน์หรือยัง
        if (Input.GetKeyDown(KeyCode.Q) && Time.time - lastDodgeTime > dodgeCooldown)
        {
            animator.Play("DodgeFrontAnimation"); // เล่นแอนิเมชันหลบ

            Vector3 dodgeDirection = transform.forward * dodgeDistance; // คำนวณทิศทางหลบ (ไปข้างหน้า)
            characterController.Move(dodgeDirection); // สั่งหลบโดยการ Move ตัวละคร

            lastDodgeTime = Time.time; // บันทึกเวลาหลบล่าสุด
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log("Cannot dodge yet. Cooldown time remaining."); // แจ้งว่าหลบไม่ได้เพราะยังไม่พ้นคูลดาวน์
        }
    }

    public IEnumerator PlayHitDamageAnimation(int takeDamage)
    {
        yield return new WaitForSeconds(0.5f);

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


        animator.Play("HitDamageAnimation");
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
