using Services.EventQueue;
using Services.EventQueue.Classes;
using Services.EventQueue.Classes.EventData;
using Services.EventQueue.Events.ScriptableObjects;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private float loadingDuration = 10f;
    [SerializeField] private Image progressBarImage;
    [SerializeField] private EventId loadSceneEventId;
    [SerializeField] private AllScenes destinationSceneName;

    private float _elapsedTime;
    private bool _eventPublished;

    private void Start()
    {
        progressBarImage.fillAmount = 0f;
    }
    
    private void Update()
    {
        if (_eventPublished)
        {
            return;
        }

        _elapsedTime += Time.deltaTime;

        var normalizedProgress = GetNormalizedProgress();
        progressBarImage.fillAmount = normalizedProgress;

        if (normalizedProgress < 1f)
        {
            return;
        }

        PublishLoadSceneEvent();
    }

    private float GetNormalizedProgress()
    {
        // A duration of zero or less would divide by zero and would never let the bar
        // visibly fill, so treat it as "already complete" instead of stalling forever.
        if (loadingDuration <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(_elapsedTime / loadingDuration);
    }

    private void PublishLoadSceneEvent()
    {
        if (_eventPublished)
        {
            return;
        }

        progressBarImage.fillAmount = 1f;
        _eventPublished = true;

        
        var eventArgs = new IntegerEventData((int)destinationSceneName);
        ServiceLocator.GetService<EventQueue>().EnqueueEvent(loadSceneEventId, eventArgs);
    }
}
