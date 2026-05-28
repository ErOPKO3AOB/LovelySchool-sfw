using UnityEngine;

namespace Assets.Scripts.Extensions
{
    public class ObjectCounter : MonoBehaviour
    {
        [ContextMenu("Count static objects")]
        private void CountStatic()
        {
            GameObject[] gameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            int count = 0;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (gameObjects[i].GetComponent<Rigidbody>() == null)
                    count++;
            }

            Debug.Log($"There are {count} static gameobjects on this scene");
        }


        [ContextMenu("Count dynamic objects")]
        private void CountDynamic()
        {
            int count = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Length;

            Debug.Log($"There are {count} dynamic gameobjects on this scene");
        }
    }
}