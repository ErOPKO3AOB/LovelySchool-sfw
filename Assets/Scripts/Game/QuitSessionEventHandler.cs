using UnityEngine;
using UnityEngine.Events;
using YG;

// Скрипт находится на сцене уровня, его метод вызывает Plugin YG
public class QuitSessionEventHandler : MonoBehaviour
{
    public UnityAction OnQuit;

    private void OnValidate()
    {
        gameObject.name = nameof(QuitSessionEventHandler);
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void OnQuitGame()
    {
        YG2.SaveProgress();

        OnQuit?.Invoke();
        OnQuit = null;
    }
}
