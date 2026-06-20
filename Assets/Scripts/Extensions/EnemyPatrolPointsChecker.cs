#if UNITY_EDITOR
using Game;
using UnityEngine;

public class EnemyPatrolPointsChecker : MonoBehaviour
{
    [ContextMenu(nameof(Check))]
    public void Check()
    {
        var spawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None);
        int counter = 0;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if ((spawnPoints[i]._patrolPoints == null || spawnPoints[i]._patrolPoints.Length <= 0))
            {
                counter++;
            }
        }

        Debug.Log($"There are {counter} spawn points without patrol points!");
    }
}
#endif