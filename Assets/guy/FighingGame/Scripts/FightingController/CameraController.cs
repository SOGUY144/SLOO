using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// สคริปต์นี้ใช้สำหรับให้กล้องติดตาม target (เช่น ตัวละคร) ที่กำลัง Active อยู่ในซีน
public class CameraFollow : MonoBehaviour
{
    // Array ของ Transform ที่กล้องสามารถเลือกติดตามได้
    public Transform[] targets;

    // ค่าความเร็วในการเคลื่อนที่ของกล้อง (ใช้สำหรับการ lerp ให้กล้องเคลื่อนที่แบบนุ่มนวล)
    public float smonthSpeed = 0.125f;

    // ระยะ offset ที่กล้องจะอยู่ห่างจาก target
    public Vector3 offset;

    // ฟังก์ชัน LateUpdate() จะถูกเรียกหลังจาก Update() ทุกเฟรม เหมาะสำหรับกล้องที่ติดตามวัตถุ
    void LateUpdate()
    {
        // ถ้ายังไม่ได้กำหนด target หรือไม่มี target เลย ก็ไม่ต้องทำอะไร
        if (targets == null || targets.Length == 0)
        {
            return;
        }

        // หาว่า target ตัวใดที่กำลัง Active อยู่
        Transform activeTarget = FindActiveTarget();

        // ถ้าไม่มี target ที่ Active ก็ไม่ต้องทำอะไร
        if (activeTarget == null)
            return;

        // ตำแหน่งที่กล้องต้องการจะเคลื่อนที่ไป (คือ ตำแหน่งของ target + offset ที่กำหนด)
        Vector3 desiredPosition = activeTarget.position + offset;

        // ล็อกตำแหน่ง Y ของกล้องไว้ ไม่ให้เปลี่ยน (เช่น กรณีกล้อง 2D มองด้านข้าง)
        desiredPosition.y = transform.position.y;

        // เคลื่อนกล้องจากตำแหน่งปัจจุบันไปยัง desiredPosition แบบนุ่มนวล ด้วยการ Lerp
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smonthSpeed);

        // อัปเดตตำแหน่งของกล้อง
        transform.position = smoothedPosition;
    }

    // ฟังก์ชันนี้ใช้วนลูปเพื่อหาว่า target ตัวใดกำลัง Active อยู่ใน Hierarchy
    Transform FindActiveTarget()
    {
        foreach (Transform target in targets)
        {
            // ถ้า GameObject ของ target นี้กำลัง Active ให้ return ออกไปทันที
            if (target.gameObject.activeInHierarchy)
                return target;
        }

        // ถ้าไม่มี target ไหนที่ Active เลย ก็ return null
        return null;
    }
}
