using PlayerPrefs = RedefineYG.PlayerPrefs;
using UnityEngine;

public class PlayerPrefsSaver : MonoBehaviour
{
    private void OnValidate()
    {
        gameObject.name = nameof(PlayerPrefsSaver);
    }
    
    public void OnQuitGame()
    {
        PlayerPrefs.Save();
    }
}
