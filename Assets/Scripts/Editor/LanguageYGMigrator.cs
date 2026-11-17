#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LanguageYGMigrator
{
    [MenuItem("Tools/Migrate LanguageYG to LocalizedText")]
    public static void MigrateAll()
    {
        int sceneCount = MigrateActiveScene();
        int prefabCount = MigrateAllPrefabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<b>[Migration]</b> Готово! Заменено на сцене: {sceneCount}, в префабах: {prefabCount}");
    }

    private static int MigrateActiveScene()
    {
        int count = 0;
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (GameObject root in rootObjects)
        {
            if (root != null)
                count += ProcessHierarchy(root);
        }

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        return count;
    }

    private static int MigrateAllPrefabs()
    {
        int count = 0;
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);

            if (prefabRoot == null) continue;

            int replacedInPrefab = ProcessHierarchy(prefabRoot);
            if (replacedInPrefab > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                count += replacedInPrefab;
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        return count;
    }

    private static int ProcessHierarchy(GameObject root)
    {
        int replaced = 0;
        if (root == null) return 0;

        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour mb in behaviours)
        {
            // Защита от Null и Missing-скриптов
            if (mb == null) continue;

            Type type = null;
            try
            {
                type = mb.GetType();
            }
            catch
            {
                continue; // Пропускаем поврежденные компоненты
            }

            if (type == null) continue;

            // Проверяем имя класса Яндекса
            if (type.Name != "LanguageYG" && type.Name != "LangYGAdditionalText")
                continue;

            string ruText = GetFieldOrPropertyString(type, mb, "ru");
            string enText = GetFieldOrPropertyString(type, mb, "en");

            GameObject go = null;
            try
            {
                go = mb.gameObject;
            }
            catch
            {
                continue;
            }

            if (go == null) continue;

            LocalizedText localized = go.GetComponent<LocalizedText>();
            if (localized == null)
            {
                localized = Undo.AddComponent<LocalizedText>(go);
            }

            if (localized != null)
            {
                localized.SetTranslations(ruText, enText);
                EditorUtility.SetDirty(localized);
            }

            Undo.DestroyObjectImmediate(mb);
            EditorUtility.SetDirty(go);

            replaced++;
        }

        return replaced;
    }

    private static string GetFieldOrPropertyString(Type type, object instance, string memberName)
    {
        try
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase;

            FieldInfo field = type.GetField(memberName, flags);
            if (field != null && field.FieldType == typeof(string))
            {
                return (string)field.GetValue(instance) ?? string.Empty;
            }

            PropertyInfo prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.PropertyType == typeof(string))
            {
                return (string)prop.GetValue(instance) ?? string.Empty;
            }
        }
        catch
        {
            // игнорируем ошибки рефлексии
        }

        return string.Empty;
    }
}
#endif