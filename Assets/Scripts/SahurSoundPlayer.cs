using UnityEngine;

public class SahurSoundPlayer : MonoBehaviour
{
    [Header("Настройки аудио")]
    public AudioSource audioSource;


    // Вызывай этот метод из других скриптов или событий (например, при получении урона)
    public void TryPlaySahur()
    {
        audioSource.Play();
    }
}