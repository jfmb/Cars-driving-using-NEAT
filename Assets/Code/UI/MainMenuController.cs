using Services.EventQueue;
using Services.EventQueue.Classes.EventData;
using Services.EventQueue.Events.ScriptableObjects;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private const string GameScenePath = "Assets/Scenes/Scene02.unity";

    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsMenuPanel;
    [SerializeField] private GameObject confirmationMenuPanel;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private SelectableNavigationGroup mainMenuNavigationGroup;
    [SerializeField] private SelectableNavigationGroup settingsNavigationGroup;
    [SerializeField] private SelectableNavigationGroup confirmationNavigationGroup;
    [SerializeField] private EventId loadSceneEventId;

    private SettingsMenuController _settingsMenuController;
    private GameObject _lastMainMenuSelection;
    private GameObject _lastSettingsMenuSelection;
    private GameObject _lastConfirmationSelection;
//    private SaveGameService _saveGameService;

    public bool IsSettingsMenuOpen => settingsMenuPanel.activeInHierarchy;
    public bool IsConfirmationMenuOpen => confirmationMenuPanel.activeInHierarchy;

    private void Awake()
    {
        Assert.IsNotNull(mainMenuPanel, "mainMenuPanel must be != null");
        Assert.IsNotNull(settingsMenuPanel, "settingsMenuPanel must be != null");
        Assert.IsNotNull(confirmationMenuPanel, "confirmationMenuPanel must be != null");
        Assert.IsNotNull(continueButton, "continueButton must be != null");
        Assert.IsNotNull(confirmationNavigationGroup, "confirmationNavigationGroup must be != null");
        Assert.IsNotNull(loadSceneEventId, "loadSceneEventId must be != null");

        if (!settingsMenuPanel.TryGetComponent(out _settingsMenuController))
        {
            Debug.LogError("MainMenuController: SettingsMenuController is missing from the settings panel.");
        }
    }

    private void Start()
    {
        if (_settingsMenuController != null)
        {
            _settingsMenuController.InitializeSettings();
        }

        if (mainMenuNavigationGroup == null)
        {
            return;
        }
        _lastMainMenuSelection = mainMenuNavigationGroup.CurrentSelection;
    }

    public void OpenSettings()
    {
        RememberSelection(mainMenuNavigationGroup, ref _lastMainMenuSelection);
        mainMenuPanel.SetActive(false);
        settingsMenuPanel.SetActive(true);
        
        if (settingsNavigationGroup == null)
        {
            return;
        }
        settingsNavigationGroup.Select(_lastSettingsMenuSelection);
    }

    private static void RememberSelection(SelectableNavigationGroup navigationGroup, ref GameObject lastSelection)
    {
        if (navigationGroup == null)
        {
            return;
        }

        var selection = navigationGroup.CurrentSelection;
        if (selection != null)
        {
            lastSelection = selection;
        }
    }
    
    public void ShowMainMenu()
    {
        RememberSelection(settingsNavigationGroup, ref _lastSettingsMenuSelection);
        RememberSelection(confirmationNavigationGroup, ref _lastConfirmationSelection);
        settingsMenuPanel.SetActive(false);
        confirmationMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
        
        if (mainMenuNavigationGroup != null)
        {
            mainMenuNavigationGroup.Select(_lastMainMenuSelection);
        }
        _lastMainMenuSelection = mainMenuNavigationGroup != null
            ? mainMenuNavigationGroup.CurrentSelection
            : null;
    }

    public void OpenQuitConfirmation()
    {
        RememberSelection(mainMenuNavigationGroup, ref _lastMainMenuSelection);
        mainMenuPanel.SetActive(false);
        confirmationMenuPanel.SetActive(true);
        confirmationNavigationGroup.Select(_lastConfirmationSelection);
        _lastConfirmationSelection = confirmationNavigationGroup.CurrentSelection;
    }

    public void StartNewGame()
    {
        var buildIndex = SceneUtility.GetBuildIndexByScenePath(GameScenePath);
        if (buildIndex < 0)
        {
            Debug.LogError($"MainMenuController: scene '{GameScenePath}' is not in Build Settings.");
            return;
        }

        var eventData = new IntegerEventData(buildIndex);
        ServiceLocator.GetService<EventQueue>().EnqueueEvent(loadSceneEventId, eventData);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


}
