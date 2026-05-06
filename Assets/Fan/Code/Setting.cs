using UnityEngine;

public class SettingPanel : MonoBehaviour
{
    [Header("Setting Panel")]
    [SerializeField] private GameObject settingPanel; // ลาก Panel Setting ใส่

    // เรียกจากปุ่ม "Setting"
    public void OpenSetting()
    {
        settingPanel.SetActive(true);
    }

    // เรียกจากปุ่ม "BACK" ใน Panel
    public void CloseSetting()
    {
        settingPanel.SetActive(false);
    }
}