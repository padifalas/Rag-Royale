using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [SerializeField] private GameObject loaderCanvas;
    [SerializeField] private Slider progressBar;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            loaderCanvas.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }

    }

    public async void LoadScene(string sceneName)
    {
        loaderCanvas.SetActive(true);
        progressBar.value = 0f;

        AsyncOperation scene = SceneManager.LoadSceneAsync(sceneName);
        scene.allowSceneActivation = false;

        float displayedProgress = 0f;

        while (displayedProgress < 1f)
        {
            displayedProgress += Time.deltaTime * 0.8f;
            progressBar.value = displayedProgress;

            await Task.Yield();
        }

        scene.allowSceneActivation = true;

        await Task.Delay(200);

        loaderCanvas.SetActive(false);


    }
}
