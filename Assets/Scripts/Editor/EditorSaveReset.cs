#if UNITY_EDITOR
using UniWebKit;
using UnityEditor;
using UnityEngine;

public static class EditorSaveReset
{
    [MenuItem("Tools/Game/Reset local saves")]
    private static void ResetLocalSaves() {
        var settings = Resources.Load<UniWebSettings>(UniWebSettings.ResourceName);
        IPlayerProvider player;
#if UNIWEB_VK_GAMES
        if (settings != null && settings.CurrentPlatform == UniWebSettings.TargetPlatform.VKGames)
            player = new VKGamesPlayerProvider(settings.PlayerStorage);
        else
#endif
            player = new LocalPrefsPlayerProvider(settings?.PlayerStorage);
        player.Load(result => {
            if (!result.IsSuccess) { Debug.LogError(result.Error); player.Dispose(); return; }
            player.DeleteAll();
            player.Save(saved => {
                if (!saved.IsSuccess) Debug.LogError(saved.Error);
                player.Dispose();
            }, PlayerStorageMode.LocalOnly);
        }, PlayerStorageMode.LocalOnly);
        PlayerPrefs.DeleteKey("Language");
        PlayerPrefs.Save();
        Debug.Log("Local fallback saves reset. Platform saves and purchases are unchanged.");
    }
    [MenuItem("Tools/Game/Reset local saves", true)]
    private static bool CanReset() => !EditorApplication.isPlayingOrWillChangePlaymode;
}
#endif
