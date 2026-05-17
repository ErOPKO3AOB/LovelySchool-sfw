using System.Linq;
using UnityEngine;

namespace Extensions
{
    public class ObjectCounter : MonoBehaviour
    {
        [ContextMenu("Count all objects")]
        private void CountAll()
        {
            int count = FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;

            Debug.Log($"There are {count} gameobjects on this scene");
        }

        [ContextMenu("Count active objects")]
        private void CountActive()
        {
            int count = 0;
            FindObjectsByType<GameObject>(FindObjectsSortMode.None).ToList().ForEach(g => { if (g.activeSelf) count++; });

            Debug.Log($"There are {count} active gameobjects on this scene");
        }
    }
}