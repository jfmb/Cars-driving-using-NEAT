using System.Collections;
using System.Collections.Generic;
using Services.EventQueue;
using Services.EventQueue.Classes;
using Services.EventQueue.Classes.EventData;
using Services.EventQueue.Events.ScriptableObjects;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneEventListener : MonoBehaviour
{
    // AsyncOperation.progress reports at most 0.9 while the scene loads in the
    // background; the remaining 0.1 happens during scene activation.
    private const float SceneActivationProgress = 0.9f;

    [SerializeField] private EventId loadSceneEventId;
    [SerializeField] private List<GameObject> gameObjectsToToggle = new();
    [SerializeField] private Image progressBarImage;

    private bool _sceneLoadRequested;

    private void OnEnable()
    {
        var loadSceneEvent =
            (IntegerEvent)ServiceLocator.GetService<EventQueue>().GetEventWithEventId(loadSceneEventId);
        loadSceneEvent.IntegerEventSender += OnLoadSceneRequested;
    }

    private void OnLoadSceneRequested(object source, IntegerEventData args)
    {
        if (_sceneLoadRequested)
        {
            return;
        }

        var buildIndex = args.Value;
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"LoadSceneEventListener: received invalid scene build index {buildIndex}. " +
                            "Scene not loaded.");
            return;
        }

        _sceneLoadRequested = true;
        ToggleConfiguredGameObjects(true);
        StartCoroutine(LoadSceneAsync(buildIndex));
    }

    private void ToggleConfiguredGameObjects(bool shouldActivate)
    {
        foreach (var gameObjectToToggle in gameObjectsToToggle)
        {
            if (gameObjectToToggle != null)
            {
                gameObjectToToggle.SetActive(shouldActivate);
            }
        }
    }

    private IEnumerator LoadSceneAsync(int buildIndex)
    {
        var asyncOperation = SceneManager.LoadSceneAsync(buildIndex);
        if (asyncOperation == null)
        {
            Debug.LogError($"LoadSceneEventListener: asynchronous loading of the scene with build index " +
                            $"{buildIndex} could not be started. Scene not loaded.");
            yield break;
        }

        // allowSceneActivation is left at its default (true) so the scene activates as soon
        // as it is ready; the progress is normalized from the 0..0.9 range to 0..1.
        while (!asyncOperation.isDone)
        {
            if (progressBarImage != null)
            {
                progressBarImage.fillAmount =
                    Mathf.Clamp01(asyncOperation.progress / SceneActivationProgress);
                Debug.Log("Loading: " + progressBarImage.fillAmount);
            }

            yield return null;
        }

        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = 1f;
        }
    }

    private void Start()
    {
        ToggleConfiguredGameObjects(false);

        if (progressBarImage != null)
        {
            progressBarImage.fillAmount = 0f;
        }
    }

    private void OnDisable()
    {
        var loadSceneEvent =
            (IntegerEvent)ServiceLocator.GetService<EventQueue>().GetEventWithEventId(loadSceneEventId);
        loadSceneEvent.IntegerEventSender -= OnLoadSceneRequested;
    }
}
