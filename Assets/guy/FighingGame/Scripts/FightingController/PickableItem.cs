using UnityEngine;

public class PickableItem : MonoBehaviour
{
    [Header("Item Settings")]
    public string itemName = "Weapon";
    [Tooltip("เปิดใช้งานระบบจับเสียง (Voice Magic) เมื่อถือไอเทมชิ้นนี้")]
    public bool isMagicVoiceItem = false;
    // กำหนดดาเมจของอาวุธแยกตามแต่ละปุ่ม (0=H, 1=J, 2=K, 3=L)
    public int[] weaponAttackDamages = { 15, 20, 25, 30 };
    public KnockbackType[] weaponKnockbackTypes = { KnockbackType.None, KnockbackType.None, KnockbackType.Pushback, KnockbackType.Knockdown };
    public float[] weaponKnockbackPowers = { 0f, 0f, 2f, 5f };
    
    [Header("Animations")]
    public string[] weaponAttackAnimations = {"Attack_Weapon1", "Attack_Weapon2", "Attack_Weapon3", "Attack_Weapon4"};
    public AudioClip[] weaponAttackSounds;

    private Rigidbody rb;
    private Collider col;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    // เมื่อถูกหยิบ จะปิดฟิสิกส์เพื่อให้ติดไปกับมือ
    public void OnPickedUp(Transform handTransform)
    {
        if (rb) rb.isKinematic = true;
        if (col) col.enabled = false;
        
        transform.SetParent(handTransform);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    // (เผื่อในอนาคต) เมื่อถูกทิ้ง จะเปิดฟิสิกส์คืน
    public void OnDropped()
    {
        transform.SetParent(null);
        if (rb) rb.isKinematic = false;
        if (col) col.enabled = true;
    }
}
