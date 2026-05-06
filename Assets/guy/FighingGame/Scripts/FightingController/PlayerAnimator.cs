using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("PlayerAnimator: ไม่พบ Animator component!");
    }

    public void SetWalking(bool isWalking)
    {
        animator.SetBool("Walking", isWalking);
    }

    // เปลี่ยนมาใช้ CrossFade เพื่อให้แอนิเมชันสมูทและลดการขัดจังหวะจากระบบอื่น
    public void PlayAttack(string animationName)
    {
        if (animator != null) animator.CrossFade(animationName, 0.1f, 0, 0f);
    }

    public void PlayHit()
    {
        // ใช้ CrossFade แทน Play เพื่อบังคับให้เปลี่ยนท่าแม้จะติด Transition อื่นอยู่
        if (animator != null) animator.CrossFade("HitDamageAnimation", 0.1f, 0, 0f);
    }

    public void PlayPushback()
    {
        if (animator != null) animator.CrossFade("PushbackAnimation", 0.1f, 0, 0f);
    }

    public void PlayFall()
    {
        // ไม่มี Falling_Down ใน Animator → ใช้ HitDamageAnimation แทน
        if (animator != null) animator.CrossFade("HitDamageAnimation", 0.1f, 0, 0f);
    }

    public void PlayGetUp()
    {
        // ชื่อจริงใน Animator คือ GetUpAnimation
        if (animator != null) animator.CrossFade("GetUpAnimation", 0.1f, 0, 0f);
    }

    public void PlayDodge()
    {
        if (animator != null) animator.CrossFade("DodgeFrontAnimation", 0.1f, 0, 0f);
    }

    public void PlayDie()
    {
        // ไม่มี Falling_Down ใน Animator → ใช้ HitDamageAnimation แทน
        if (animator != null) animator.CrossFade("HitDamageAnimation", 0.1f, 0, 0f);
    }

    public void PlayIdle()
    {
        // บังคับกลับ Idle หลัง knockdown recovery
        if (animator != null) animator.CrossFade("IdleAnimation", 0.1f, 0, 0f);
    }
}