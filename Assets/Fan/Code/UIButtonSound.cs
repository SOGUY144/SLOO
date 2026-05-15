using UnityEngine;

public class UIButtonSound : MonoBehaviour
{
    [SerializeField] private AudioSource clickSound;

    public void PlaySound()
    {
        if (clickSound != null)
        {
            clickSound.Play();
        }
    }
}