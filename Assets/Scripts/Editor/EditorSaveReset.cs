#if UNITY_EDITOR
using UniWebKit;
using UnityEditor;
using UnityEngine;

public static class EditorSaveReset
{
    [MenuItem("Tools/Game/Reset local saves")]
    private static void ResetLocalSaves() {
        var settings = Resources.Load<UniWebSettings>(UniWebSettings.ResourceName);
        var player = new LocalPrefsPlayerProvider(settings?.PlayerStorage);
        player.Load(result => {
            if (result.IsSuccess) { player.DeleteAll(); player.Save(); }
            player.Dispose();
        });
        PlayerPrefs.DeleteKey("Language");
        PlayerPrefs.Save();
        Debug.Log("Local fallback saves reset. Platform saves and purchases are unchanged.");
    }
    [MenuItem("Tools/Game/Reset local saves", true)]
    private static bool CanReset() => !EditorApplication.isPlayingOrWillChangePlaymode;
}
#endif
