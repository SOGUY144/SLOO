using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthBarSlider;

    // ให้พลังชีวิตเต็ม
    public void GiveFullHealth(float health)
    {
        healthBarSlider.maxValue = health;
        healthBarSlider.value = health;
    }

    // ตั้งค่าพลังชีวิตปัจจุบัน
    public void SetHealth(float health)
    {
        healthBarSlider.value = health;
    }
}
