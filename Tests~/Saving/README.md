# Save regression checks

The game keeps checkpoint snapshots: weapons, ammo, reserve and selected weapon
are captured when a checkpoint is reached. Picking up ammo or shooting does not
overwrite that checkpoint snapshot. New-game/bus reset clears inventory and progress
together. Settings persist separately.

Editor storage explicitly uses LocalOnly on the selected UniWebKit provider,
preserving its cache namespace. WebGL uses Preferred storage with local fallback.
One persistent GameSaveSession reuses loaded data across scenes and queues saves
while the provider is busy. Invalid initial storage blocks startup and offers a retry;
it never silently replaces existing data with a default save.

Inventory uses a versioned DTO under inventory_v1. Valid old lists can be read;
empty strings and the old broken {} JSON are treated as empty inventories.
An old {} contains no recoverable weapon data. Existing unrelated fields remain intact.

To rerun native Unity checks without touching real game saves, prepare an isolated
project (Python 3, Unity 6000.3.18f1 and a valid Unity license are required):

```powershell
python Tests~/Saving/prepare_checks.py --package C:\UnityPackages\UniWebKit
```

Use the printed directory as PROJECT below:

```text
Unity.exe -batchmode -nographics -noUpm -projectPath PROJECT -executeMethod LovelySchoolSaveChecks.Run -logFile PROJECT/Unity.log
```

The harness enters Play Mode, tests actual UniWebKit storage, actual game inventory
and native JsonUtility, and writes SaveChecks.txt. Other gameplay dependencies use
small test doubles. Test cache keys include a random identifier. This is not an
end-to-end run of the main scene or of the VK browser SDK.

Manual checks in the main game:

1. Pick up two weapons, spend ammo, select the first slot, then reach a checkpoint.
2. Restart the level and restart Editor Play Mode: checkpoint, weapons, selected
   slot and checkpoint ammo must match. No duplicate inventory slots should appear.
3. Change sensitivity, music and hints; close the pause menu and restart Play Mode.
4. Test rewarded checkpoint skip and the bus/new-game reset.
5. In a VK WebGL build, check reloads, a slow connection and local fallback.
