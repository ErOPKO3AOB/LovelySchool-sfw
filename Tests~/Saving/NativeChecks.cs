using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game;
using UniWebKit;
using UnityEngine;

public static class LovelySchoolSaveChecks
{
    [UnityEditor.InitializeOnLoadMethod]
    private static void ResumeInPlayMode()
    {
        if (UnityEditor.SessionState.GetBool("LovelySchoolSaveChecks", false))
            UnityEditor.EditorApplication.delayCall += WaitForPlayMode;
    }
    private static void WaitForPlayMode()
    {
        if (!Application.isPlaying) { UnityEditor.EditorApplication.delayCall += WaitForPlayMode; return; }
        UnityEditor.SessionState.EraseBool("LovelySchoolSaveChecks");
        Run();
    }
    private static int _checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        _checks++;
    }
    public static void Run()
    {
        if (!Application.isPlaying)
        {
            UnityEditor.SessionState.SetBool("LovelySchoolSaveChecks", true);
            UnityEditor.EditorApplication.EnterPlaymode();
            return;
        }
        try
        {
            var options = new PlayerStorageOptions { CloudKey = "LovelySchoolValidation_" + Guid.NewGuid().ToString("N") };
            using (var player = new VKGamesPlayerProvider(options))
            {
                var loaded = player.Load(mode: PlayerStorageMode.LocalOnly);
                Check(loaded.IsCompleted && loaded.Result.IsSuccess && player.IsReady, "VK Editor local load completes synchronously");
                Check(GameObject.FindObjectsByType<PlayerStorageRuntime>(FindObjectsSortMode.None).Length == 0, "Local load never creates VK SDK waiter");
                Check(!player.CanConfirmCloudSaves, "Editor does not claim cloud confirmation");
                var data = new InventorySaveData { selectedWeaponName = "Rifle", weapons = new List<WeaponData> {
                    new WeaponData { weaponName = "Knife", ammo = 0, reserve = 0 },
                    new WeaponData { weaponName = "Rifle", ammo = 7, reserve = 23 }
                }};
                Check(player.Set(InventorySaveData.Key, data), "Unity accepts typed inventory DTO");
                Check(player.Set("currentCheckPoint", 3), "Checkpoint accepted");
                Check(player.Save(mode: PlayerStorageMode.LocalOnly).Result.IsLocalSaved, "Snapshot is persisted");
                var restored = InventorySaveData.Read(player);
                Check(restored.weapons.Count == 2 && restored.selectedWeaponName == "Rifle", "Weapon order and selected slot round-trip through native JsonUtility");
                Check(restored.weapons[1].ammo == 7 && restored.weapons[1].reserve == 23, "Ammo and reserve preserved");
                restored.weapons[1].ammo = 999;
                Check(InventorySaveData.Read(player).weapons[1].ammo == 7, "Restored objects do not mutate saved snapshot");
                using (var fresh = new VKGamesPlayerProvider(options))
                {
                    fresh.Load(mode: PlayerStorageMode.LocalOnly);
                    Check(fresh.Get<int>("currentCheckPoint") == 3, "Checkpoint survives a new provider");
                    Check(InventorySaveData.Read(fresh).weapons[1].ammo == 7, "Weapons survive a new provider");
                }
                player.DeleteKey(InventorySaveData.Key);
                player.Set(InventorySaveData.LegacyKey, "{}");
                Check(InventorySaveData.Read(player).weapons.Count == 0, "Old broken root-list JSON is safe");
                player.Set(InventorySaveData.LegacyKey, "");
                Check(InventorySaveData.Read(player).weapons.Count == 0, "Empty legacy save is safe");
                player.Set(InventorySaveData.LegacyKey, "[{\"weaponName\":\"Rifle\",\"ammo\":4,\"reserve\":9}]");
                Check(InventorySaveData.Read(player).weapons[0].ammo == 4, "Valid legacy arrays are recovered");
                player.Set(InventorySaveData.LegacyKey, "{bad json");
                Check(InventorySaveData.Read(player).weapons.Count == 0, "Malformed legacy JSON is safe");
                player.Set(InventorySaveData.Key, data);
                player.Set("Sensitivity", .35f);
                UniWebBridge.Player = player;
                var session = GameSaveSession.Instance;
                Check(!session.EnsureLoaded().MoveNext(), "Ready save is reused without reloading scene progress");
                GameSaveSession.Set("currentCheckPoint", 4);
                GameSaveSession.Save();
                using (var fresh = new VKGamesPlayerProvider(options))
                {
                    fresh.Load(mode: PlayerStorageMode.LocalOnly);
                    Check(fresh.Get<int>("currentCheckPoint") == 4 && Mathf.Approximately(fresh.Get<float>("Sensitivity"), .35f), "Coordinator persists checkpoint and settings");
                }
                UnityEngine.Object.DestroyImmediate(session.gameObject);
                player.DeleteAll(); player.Save(mode: PlayerStorageMode.LocalOnly);
                Check(player.Keys.Count == 0, "Explicit reset clears active VK cache");
            }
            var backend = new TestBackend();
            using (var player = new CustomPlayerProvider(backend, options))
            {
                var load = player.Load();
                Check(!player.IsReady && !player.Set("currentCheckPoint", 0), "Pending load rejects writes");
                backend.ReadDone(new PlayerStorageBackendResult(cloudConfirmed: true));
                Check(load.IsCompleted && player.IsReady, "Delayed provider load completes");
                player.Set("currentCheckPoint", 5);
                player.Save();
                player.Set("currentCheckPoint", 6);
                UniWebBridge.Player = player;
                GameSaveSession.Save();
                Check(player.IsBusy, "Save coordinator queues while provider is busy");
                backend.WriteDone(new PlayerStorageBackendResult(cloudConfirmed: true));
                typeof(GameSaveSession).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(GameSaveSession.Instance, null);
                Check(!player.HasChanges, "Queued save flushes after cloud request completes");
                using (var fresh = new CustomPlayerProvider(backend, options))
                {
                    fresh.Load(mode: PlayerStorageMode.LocalOnly);
                    Check(fresh.Get<int>("currentCheckPoint") == 6, "Queued snapshot preserves newer checkpoint");
                }
                UnityEngine.Object.DestroyImmediate(GameSaveSession.Instance.gameObject);
            }
            string cacheKey = "UniWebKit.Player.vk.guest." + options.CloudKey;
            PlayerPrefs.SetString(cacheKey, "broken");
            using (var player = new VKGamesPlayerProvider(options))
            {
                Check(!player.Load(mode: PlayerStorageMode.LocalOnly).Result.IsSuccess, "Corrupt cache fails explicitly");
                Check(!player.Set("currentCheckPoint", 0) && PlayerPrefs.GetString(cacheKey) == "broken", "Corrupt progress is not overwritten with defaults");
            }
            PlayerPrefs.DeleteKey(cacheKey);
            PlayerPrefs.DeleteKey("UniWebKit.Player.test.player." + options.CloudKey);
            PlayerPrefs.Save();
            CheckInventoryRestoration(options);
            Debug.Log("PASS: " + _checks + " native Unity save checks");
            File.WriteAllText("SaveChecks.txt", "PASS: " + _checks + " native Unity save checks");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            File.WriteAllText("SaveChecks.txt", error.ToString());
            UnityEditor.EditorApplication.Exit(1);
        }
    }
    private static void CheckInventoryRestoration(PlayerStorageOptions options)
    {
        // Configure the one game layer used by the real Inventory component.
        var tagManager = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        tagManager.FindProperty("layers").GetArrayElementAtIndex(8).stringValue = "ItemInHands";
        tagManager.ApplyModifiedProperties();
        using (var player = new VKGamesPlayerProvider(options))
        {
            player.Load(mode: PlayerStorageMode.LocalOnly);
            player.Set(InventorySaveData.Key, new InventorySaveData {
                selectedWeaponName = "Knife", weapons = new List<WeaponData> {
                    new WeaponData { weaponName = "Knife", ammo = 0 },
                    new WeaponData { weaponName = "Rifle", ammo = 7, reserve = 23 },
                    new WeaponData { weaponName = "Rifle", ammo = 999, reserve = -1 },
                    null,
                    new WeaponData { weaponName = "RemovedPrefab" }
                }
            });
            UniWebBridge.Player = player;
            var host = new GameObject("Inventory test");
            var facade = host.AddComponent<Player.PlayerFacade>();
            var inventory = host.AddComponent<Player.Inventory>();
            var hand = new GameObject("Hand").transform;
            var text = new GameObject("Ammo").AddComponent<TMPro.TMP_Text>();
            var camera = new GameObject("Camera").AddComponent<Camera>();
            var spawner = new GameObject("Spawner").AddComponent<RandomPointItemsSpawner>();
            var knife = new GameObject("Knife").AddComponent<Weapon>();
            knife.gameObject.AddComponent<Rigidbody>(); knife.gameObject.AddComponent<Animator>();
            knife.weaponName = "Knife"; knife.IsMelee = true; knife.MaxAmmo = 0;
            var rifle = new GameObject("Rifle").AddComponent<Weapon>();
            rifle.gameObject.AddComponent<Rigidbody>(); rifle.gameObject.AddComponent<Animator>();
            rifle.weaponName = "Rifle";
            spawner.Weapons.Add(knife); spawner.Weapons.Add(rifle);
            foreach (var field in new Dictionary<string,object> { { "_playerFacade",facade }, { "_hand",hand }, { "_ammoText",text } })
                typeof(Player.Inventory).GetField(field.Key, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inventory,field.Value);
            inventory.Initialize(new Assets.Scripts.Input.TestInputReader(),camera,spawner);
            inventory.LoadWeaponsState();
            Check(!inventory.IsRestored && inventory.Weapons.Count == 0, "Restore waits for spawner readiness");
            spawner.IsReady = true;
            inventory.LoadWeaponsState();
            Check(inventory.IsRestored && inventory.Weapons.Count == 2, "Restore skips duplicate/missing/null weapons");
            Check(rifle.CurrentAmmo == 7 && rifle.Reserve == 23, "Actual Inventory restores ammo before pickup");
            Check(inventory.CurrentWeaponIndex == 0 && knife.gameObject.activeSelf && !rifle.gameObject.activeSelf, "Selected weapon is restored");
            inventory.LoadWeaponsState();
            Check(inventory.Weapons.Count == 2, "Repeated restoration is idempotent");
            Check(inventory.SaveWeaponsState(), "Restored inventory can capture checkpoint snapshot");
            var captured = InventorySaveData.Read(player);
            Check(captured.weapons.Count == 2 && captured.selectedWeaponName == "Knife", "Captured snapshot preserves selected weapon and order");
            Check(text.text == "0/0", "Ammo UI matches restored selected weapon");
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            UnityEngine.Object.DestroyImmediate(text.gameObject);
            UnityEngine.Object.DestroyImmediate(spawner.gameObject);
            UnityEngine.Object.DestroyImmediate(GameSaveSession.Instance.gameObject);
            player.DeleteAll(); player.Save(mode: PlayerStorageMode.LocalOnly);
        }
        PlayerPrefs.DeleteKey("UniWebKit.Player.vk.guest." + options.CloudKey);
        PlayerPrefs.Save();
    }
    private sealed class TestBackend : IPlayerStorageBackend
    {
        public Action<PlayerStorageBackendResult> ReadDone, WriteDone;
        public string Id => "test";
        public string ProfileId => "player";
        public bool IsAvailable => true;
        public bool CanConfirmCloudWrites => true;
        public PlayerStorageLocation Location => PlayerStorageLocation.Cloud;
        public void Read(string key, Action<PlayerStorageBackendResult> done) => ReadDone = done;
        public void Write(string key, string document, Action<PlayerStorageBackendResult> done) => WriteDone = done;
    }
}
