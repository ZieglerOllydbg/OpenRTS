using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    [SerializeField] private Button enterButton;
    [SerializeField] private Button testButton;
    [SerializeField] private Scrollbar progressBar;
    [SerializeField] private float debugLoadDelaySeconds;

    private const string PetsWarScenePath = "Assets/Scenes/PetsWar.unity";
    private const string SimpleRTSScenePath = "SimpleRTS.unity";
    private bool isLoading;

    private void Awake()
    {
        if (progressBar != null)
        {
            progressBar.size = 1f;
            progressBar.value = 0f;
        }

        if (enterButton != null)
        {
            enterButton.onClick.AddListener(OnClickButton);
        }

        if (testButton != null)
        {
            testButton.onClick.AddListener(OnClickTestButton);
        }
    }

    private void OnDestroy()
    {
        if (enterButton != null)
        {
            enterButton.onClick.RemoveListener(OnClickButton);
        }

        if (testButton != null)
        {
            testButton.onClick.RemoveListener(OnClickTestButton);
        }
    }

    private void Start()
    {
        StartSceneLoad(SimpleRTSScenePath);
    }

    private void OnClickButton()
    {
        StartSceneLoad(PetsWarScenePath);
    }

    private void OnClickTestButton()
    {
        StartSceneLoad(SimpleRTSScenePath);
    }

    private void StartSceneLoad(string scenePath)
    {
        if (isLoading)
        {
            return;
        }

        StartCoroutine(LoadScene(scenePath));
    }

    private System.Collections.IEnumerator LoadScene(string scenePath)
    {
        isLoading = true;
        SetSliderProgress(0f);

        AsyncOperationHandle<SceneInstance> handle =
            Addressables.LoadSceneAsync(scenePath, LoadSceneMode.Single, false);

        handle.Completed += obj =>
        {
            Debug.LogWarning($"Load async scene complete {scenePath} {obj.Status}");
        };

        while (!handle.IsDone)
        {
            SetSliderProgress(handle.PercentComplete);
            yield return null;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            isLoading = false;
            yield break;
        }

        SetSliderProgress(1f);
        if (debugLoadDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(debugLoadDelaySeconds);
        }

        AsyncOperation activateOperation = handle.Result.ActivateAsync();
        while (!activateOperation.isDone)
        {
            yield return null;
        }

        isLoading = false;
    }

    private void SetSliderProgress(float progress)
    {
        if (progressBar == null)
        {
            return;
        }

        progressBar.value = Mathf.Clamp01(progress);
    }
}
