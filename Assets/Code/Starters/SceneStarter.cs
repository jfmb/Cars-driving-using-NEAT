using Services.EventQueue;
using Services.EventQueue.Events.ScriptableObjects;
using UnityEngine;
using UnityEngine.Assertions;

// Run after ordinary Awake callbacks so SceneSaver can restore before scene starters execute.
[DefaultExecutionOrder(100)]
public class SceneStarter : MonoBehaviour
{
    [SerializeField] private EventId dataLoaderCompletedEventId; 
    [SerializeField] private FeatureStarter[] startersAtAwake;
    [SerializeField] private FeatureStarter[] startersAtStart;

    private SimpleEvent _dataLoaderCompletedEvent;
    private bool _awakeStartersExecuted;

    private void Awake()
    {
        _dataLoaderCompletedEvent = (SimpleEvent)ServiceLocator.GetService<EventQueue>()
            .GetEventWithEventId(dataLoaderCompletedEventId);
        Assert.IsNotNull(_dataLoaderCompletedEvent, "DataLoaderCompleted event must be installed");

        _dataLoaderCompletedEvent.SimpleEventSender += OnDataLoaderCompleted;
    }

    private void OnDataLoaderCompleted()
    {
        if (_awakeStartersExecuted)
        {
            return;
        }

        Debug.Log("Starting features at Awake...");
        _awakeStartersExecuted = true;
        _dataLoaderCompletedEvent.SimpleEventSender -= OnDataLoaderCompleted;
        StartFeatures(startersAtAwake);
    }

    private void StartFeatures(FeatureStarter[] starters)
    {
        if (starters == null)
        {
            return;
        }

        foreach (FeatureStarter starter in starters)
        {
            if (starter == null)
            {
                continue;
            }

            starter.StartFeature();
        }
    }

    private void Start()
    {
        StartFeatures(startersAtStart);
    }

    private void OnDestroy()
    {
        if (_dataLoaderCompletedEvent != null)
        {
            _dataLoaderCompletedEvent.SimpleEventSender -= OnDataLoaderCompleted;
        }
    }
}
