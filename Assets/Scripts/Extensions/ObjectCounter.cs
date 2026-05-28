using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Extensions
{
    public class ObjectCounter : MonoBehaviour
    {
        [ContextMenu("Count static objects")]
        private void CountStatic()
        {
            int count = FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;

            Debug.Log($"There are {count} dynamic gameobjects on this scene");
        }

        
        [ContextMenu("Count dynamic objects")]
        private void CountDynamic()
        {
            int count = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length;

            Debug.Log($"There are {count} dynamic gameobjects on this scene");
        }
    }
}