using System.Collections;
using UnityEngine;

public class SahurSoundPlayer : MonoBehaviour
{
    [Header("Настройки аудио")]
    public AudioSource audioSource;

    [Header("Настройки логики")]
    [Tooltip("Шанс срабатывания звука от 0 до 100%")]
    [Range(0f, 100f)]
    public float chance = 50f; // По умолчанию шанс 50%


    // Вызывай этот метод из других скриптов или событий (например, при получении урона)
    public void TryPlaySahur()
    {
        // Генерируем случайное число от 0 до 100
        float randomValue = Random.Range(0f, 100f);

        // Проверяем, попали ли мы в заданный шанс
        if (randomValue <= chance)
        {
            // Если шанс прокнул, запускаем корутину с задержкой
            audioSource.Play();
        }
    }
}