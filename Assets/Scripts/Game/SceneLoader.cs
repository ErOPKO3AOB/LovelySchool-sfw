using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string sceneName;
    [SerializeField] private bool _invokeFromStart;
    [SerializeField] private int _startInvokeDelay;

    private void Start()
    {
        if (_invokeFromStart)
        {
            StartCoroutine(LoadSceneCoroutine());
        }
    }

    public void LoadScene()
    {
        StartCoroutine(LoadSceneCoroutine());
    }

    private IEnumerator LoadSceneCoroutine()
    {
        if (_startInvokeDelay > 0)
            yield return new WaitForSeconds(_startInvokeDelay);

        yield return SceneManager.LoadSceneAsync(sceneName);
    }
}