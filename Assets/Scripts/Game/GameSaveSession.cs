using System;
using System.Collections;
using UniWebKit;
using UnityEngine;

// Owns one loaded player across scenes and serializes save requests.
public sealed class GameSaveSession : MonoBehaviour
{
#if UNITY_EDITOR
    private const PlayerStorageMode StorageMode = PlayerStorageMode.LocalOnly;
#else
    private const PlayerStorageMode StorageMode = PlayerStorageMode.Preferred;
#endif
    private const double StartupLoadTimeoutSeconds = 8;
    private static GameSaveSession _instance;
    private bool _loading, _saveRequested, _saving, _quitting;
    private string _loadError;
    private float _retryAt;
    public static GameSaveSession Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var host = new GameObject("Game saves");
            DontDestroyOnLoad(host);
            return _instance = host.AddComponent<GameSaveSession>();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => _instance = null;

    public IEnumerator EnsureLoaded()
    {
        while (!UniWebBridge.Player.IsReady)
        {
            if (!_loading && _loadError == null && !UniWebBridge.Player.IsBusy)
            {
                _loading = true;
                StartCoroutine(LoadProgress(UniWebBridge.Player));
            }
            if (!UniWebBridge.Player.IsReady) yield return null;
        }
    }

    private IEnumerator LoadProgress(IPlayerProvider player)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + StartupLoadTimeoutSeconds;
        var operation = player.Load(mode: StorageMode);
        while (!operation.IsCompleted && Time.realtimeSinceStartupAsDouble < deadline)
            yield return null;

        if (!operation.IsCompleted)
        {
            // A late cloud reply must not replace progress once gameplay has started.
            operation.Cancel();
            Debug.LogWarning("[Saves] Cloud load timed out at startup. Loading local progress for this session.");
            operation = player.Load(mode: PlayerStorageMode.LocalOnly);
        }

        while (!operation.IsCompleted) yield return null;
        _loading = false;
        var result = operation.Result;
        if (!result.IsSuccess && !player.IsReady)
        {
            _loadError = result.Error?.ToString() ?? "Unknown storage error";
            Debug.LogError("[Saves] " + _loadError);
        }
        else if (!result.IsSuccess || result.IsFallback)
            Debug.LogWarning("[Saves] Using local progress: " + (result.Error ?? result.CloudError));
    }

    public static bool Set<T>(string key, T value)
    {
        bool accepted = UniWebBridge.Player.Set(key, value);
        if (!accepted) Debug.LogError("[Saves] Could not update " + key + ". Player data is not ready or the value cannot be serialized.");
        return accepted;
    }

    public static void Save()
    {
        if (!UniWebBridge.Player.IsReady) return;
        var session = Instance;
        session._saveRequested = true;
        session.Flush();
    }

    private void Update()
    {
        if (_saveRequested && Time.realtimeSinceStartup >= _retryAt) Flush();
    }

    private void Flush()
    {
        var player = UniWebBridge.Player;
        if (_saving || player.IsBusy || !player.IsReady) return;
        _saveRequested = false;
        _saving = true;
        player.Save(result =>
        {
            if (this == null) return;
            _saving = false;
            if (!result.IsSuccess)
            {
                Debug.LogWarning("[Saves] " + result.Error);
                _saveRequested = true;
                _retryAt = Time.realtimeSinceStartup + 5f;
            }
            // Changes made during an in-flight save need their own snapshot.
            _saveRequested |= player.HasChanges;
        }, StorageMode);
    }

    private void OnApplicationFocus(bool focused) { if (!focused && UniWebBridge.Player.HasChanges) Save(); }
    private void OnApplicationPause(bool paused) { if (paused && UniWebBridge.Player.HasChanges) Save(); }
    private void OnApplicationQuit() { _quitting = true; if (UniWebBridge.Player.IsReady) Flush(); }

    private void OnGUI()
    {
        if (_loadError == null) return;
        var area = new Rect((Screen.width - 360) / 2f, (Screen.height - 130) / 2f, 360, 130);
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label("Не удалось загрузить прогресс. Данные не перезаписаны.");
        if (GUILayout.Button("Повторить загрузку")) _loadError = null;
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
        // Save writes its local snapshot synchronously, before any cloud request.
        if (!_quitting && UniWebBridge.Player.IsReady && UniWebBridge.Player.HasChanges && !UniWebBridge.Player.IsBusy)
            UniWebBridge.Player.Save(mode: StorageMode);
        _instance = null;
    }
}
