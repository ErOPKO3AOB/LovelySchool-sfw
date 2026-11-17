#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class GamePushMigrationValidation
{
    [MenuItem("Tools/Game/Validate UniWebKit migration")]
    public static void Run() {
        int dependencies = 0;
        foreach (var path in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
            if (Regex.IsMatch(File.ReadAllText(path), @"using Game" + @"Push;|\bGP_\w+")) {
                dependencies++; Debug.LogError("Direct SDK dependency: " + path);
            }
        Debug.Log("UniWebKit migration: direct SDK dependencies = " + dependencies);
    }
}
#endif
