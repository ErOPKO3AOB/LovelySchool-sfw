using System;
using System.Collections.Generic;
using Game;
using UniWebKit;
using UnityEngine;

[Serializable]
public sealed class InventorySaveData
{
    public const string Key = "inventory_v1";
    public const string LegacyKey = "weaponDatas";
    public int version = 1;
    public string selectedWeaponName;
    public List<WeaponData> weapons = new List<WeaponData>();

    public static InventorySaveData Read(IPlayerProvider player)
    {
        if (player.TryGet<InventorySaveData>(Key, out var current) && current != null
            && current.version == 1 && current.weapons != null) return current;
        if (player.HasKey(Key))
        {
            Debug.LogWarning("[Saves] Inventory data is invalid or belongs to another version.");
            return new InventorySaveData();
        }
        if (player.TryGet<List<WeaponData>>(LegacyKey, out var list) && list != null)
            return new InventorySaveData { weapons = list };

        var json = player.Get(LegacyKey, "");
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}") return new InventorySaveData();
        try
        {
            // Accept old valid arrays and wrapped lists; broken root-list JSON contains no recoverable items.
            if (json.TrimStart().StartsWith("[")) json = "{\"weapons\":" + json + "}";
            var legacy = JsonUtility.FromJson<InventorySaveData>(json);
            if (legacy != null && legacy.weapons != null && json.Contains("\"weapons\"")) return legacy;
        }
        catch (Exception) { }
        Debug.LogWarning("[Saves] Legacy inventory could not be restored. Other progress was preserved.");
        return new InventorySaveData();
    }
}
