using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ควบคุมการเคลื่อนที่และการโจมตีของตัวละคร (ฉบับปรับปรุงระบบ Moveset)
public class FightingController : MonoBehaviour
{
    [Header("Player Movement")]
    public float movementSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = 9.81f;
    private float verticalVelocity;
    private CharacterController characterController;
    private Animator animator;

    [Header("Player Fight")]
    public float attackCooldown = 0.5f; 
    public float dodgeCooldown = 1.5f; 
    public int attackDamages = 5; 
    // ลำดับ: 0=H, 1=J, 2=K, 3=L
    public string[] attackAnumations = {"Attack1Animation","Attack2Animation","Attack3Animation","Attack4Animation"};
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
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public ParticleSystem attack1Effect;
    public ParticleSystem attack2Effect;
    public ParticleSystem attack3Effect;
    public ParticleSystem attack4Effect;
    public AudioClip[] attackSounds;
    public AudioClip dodgeSound;
    public AudioClip[] hitSounds;
    private AudioSource audioSource;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public HealthBar healthBar;

    void Awake()
    {
        currentHealth = maxHealth;
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        // Setup AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f; // 0 = 2D Sound (เสียงดังเท่ากันไม่ว่ามุมกล้องจะอยู่ไหน)
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        PerformMovement();
        PerformDodgeFront();

        if (Input.GetKeyDown(KeyCode.H)) PerformAttack(0);
        else if (Input.GetKeyDown(KeyCode.J)) PerformAttack(1);
        else if (Input.GetKeyDown(KeyCode.K)) PerformAttack(2);
        else if (Input.GetKeyDown(KeyCode.L)) PerformAttack(3);

        if (Input.GetKeyDown(KeyCode.F)) TryPickupItem();
        if (Input.GetKeyDown(KeyCode.G)) DropItem();
    }

    void PerformMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(-verticalInput, 0f, horizontalInput);

        if (movement.magnitude > 0.1f)
        {
            movement.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            animator.SetBool("Walking", true);
        }
        else
        {
            animator.SetBool("Walking", false);
            movement = Vector3.zero;
        }

        if (characterController.isGrounded) verticalVelocity = -0.5f;
        else verticalVelocity -= gravity * Time.deltaTime;

        Vector3 moveVector = movement * movementSpeed;
        moveVector.y = verticalVelocity;
        characterController.Move(moveVector * Time.deltaTime);
    }

    void PerformAttack(int attackIndex)
    {
        if (Time.time - lastAttackTime > attackCooldown)
        {
            string animationName = "";

            if (heldItem != null)
            {
                // --- กรณีถืออาวุธ ---
                // มีท่าตามปุ่มที่กดเท่านั้น ถ้าไม่มีช่องนั้นจะไม่ทำอะไรเลย
                if (attackIndex < heldItem.weaponAttackAnimations.Length)
                {
                    animationName = heldItem.weaponAttackAnimations[attackIndex];
                }
            }
            else
            {
                // --- กรณีมือเปล่า ---
                if (attackIndex < attackAnumations.Length)
                {
                    animationName = attackAnumations[attackIndex];
                }
            }

            if (!string.IsNullOrEmpty(animationName))
            {
                animator.CrossFade(animationName, 0.1f);
                
                // เล่นเสียงการโจมตี
                AudioClip clipToPlay = null;

                if (heldItem != null)
                {
                    // กรณีถืออาวุธ: ดึงเสียงจากอาวุธมาใช้
                    if (heldItem.weaponAttackSounds != null && heldItem.weaponAttackSounds.Length > 0)
                    {
                        int soundIndex = Mathf.Clamp(attackIndex, 0, heldItem.weaponAttackSounds.Length - 1);
                        clipToPlay = heldItem.weaponAttackSounds[soundIndex];
                    }
                }
                else
                {
                    // กรณีมือเปล่า: ดึงเสียงต่อย/เตะแบบเดิม
                    if (attackSounds != null && attackSounds.Length > 0)
                    {
                        int soundIndex = Mathf.Clamp(attackIndex, 0, attackSounds.Length - 1);
                        clipToPlay = attackSounds[soundIndex];
                    }
                }

                if (clipToPlay != null)
                {
                    audioSource.PlayOneShot(clipToPlay, sfxVolume);
                }
                
                int damage = attackDamages + (heldItem != null ? heldItem.additionalDamage : 0);
                Debug.Log("Attack: " + animationName + " | Damage: " + damage);

                lastAttackTime = Time.time;

                foreach(Transform opponent in opponents)
                {
                    if(Vector3.Distance(transform.position, opponent.position) <= attackRadius)
                    {
                        opponent.GetComponent<OpponentAI>().StartCoroutine(
                            opponent.GetComponent<OpponentAI>().PlayHitDamageAnimation(damage));
                    }
                }
            }
        }
    }

    void TryPickupItem()
    {
        if (heldItem != null) return;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRange);
        foreach (var hitCollider in hitColliders)
        {
            PickableItem item = hitCollider.GetComponent<PickableItem>();
            if (item != null)
            {
                heldItem = item;
                item.OnPickedUp(handTransform);
                break;
            }
        }
    }

    void DropItem()
    {
        if (heldItem != null)
        {
            heldItem.OnDropped();
            heldItem = null;
        }
    }

    void PerformDodgeFront()
    {
        if (Input.GetKeyDown(KeyCode.Q) && Time.time - lastDodgeTime > dodgeCooldown)
        {
            animator.Play("DodgeFrontAnimation");
            
            // เล่นเสียงตอนพุ่งหลบ (ถ้ามี)
            if (dodgeSound != null)
            {
                audioSource.PlayOneShot(dodgeSound, sfxVolume);
            }
            
            Vector3 dodgeDirection = transform.forward * dodgeDistance;
            characterController.Move(dodgeDirection);
            lastDodgeTime = Time.time;
        }
    }

    public IEnumerator PlayHitDamageAnimation(int takeDamage)
    {
        yield return new WaitForSeconds(0.5f);
        if(hitSounds != null && hitSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, hitSounds.Length);
            audioSource.PlayOneShot(hitSounds[randomIndex], sfxVolume);
        }
        currentHealth -= takeDamage;
        if(healthBar != null) healthBar.SetHealth(currentHealth);
        if(currentHealth <= 0) Die();
        animator.Play("HitDamageAnimation");
    }

    void Die() { Debug.Log("Player died."); }

    public void Attack1Effect() { if(attack1Effect) attack1Effect.Play(); }
    public void Attack2Effect() { if(attack2Effect) attack2Effect.Play(); }
    public void Attack3Effect() { if(attack3Effect) attack3Effect.Play(); }
    public void Attack4Effect() { if(attack4Effect) attack4Effect.Play(); }
}