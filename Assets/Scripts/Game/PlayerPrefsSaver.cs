using UnityEngine;
using YG;

// Скрипт находится на сцене уровня, его метод вызывает Plugin YG
public class PlayerPrefsSaver : MonoBehaviour
{
    private void OnValidate()
    {
        gameObject.name = nameof(PlayerPrefsSaver);
    }
    
    public void OnQuitGame()
    {
        YG2.SaveProgress();
    }
}
