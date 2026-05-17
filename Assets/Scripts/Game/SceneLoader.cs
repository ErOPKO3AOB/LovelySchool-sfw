using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string sceneName;
    [SerializeField] private bool _invokeFromStart;
    [SerializeField] private int _startInvokeDelay;

    private async void Start()
    {
        if (_invokeFromStart)
        {
            await LoadScene();
        }
    }

    public async Task LoadScene()
    {
        if (_startInvokeDelay > 0)
            await Task.Delay(_startInvokeDelay * 1000);

        await SceneManager.LoadSceneAsync(sceneName);
    }
}
