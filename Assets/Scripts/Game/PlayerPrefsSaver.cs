using PlayerPrefs = RedefineYG.PlayerPrefs;
using UnityEngine;
using YG;

public class PlayerPrefsSaver : MonoBehaviour
{
    private void OnValidate()
    {
        gameObject.name = nameof(PlayerPrefsSaver);
    }
    
    public void OnQuitGame()
    {
        PlayerPrefs.Save();
        YG2.SaveProgress();
    }
}
