using UnityEngine;

public class SettingsServiceInstaller : GameService
{
    [SerializeField] private SettingsService settingsService;

    public override void Install()
    {
        DontDestroyOnLoad(settingsService.gameObject);
        ServiceLocator.RegisterService(settingsService);
        IsInstallationDone = true;
    }
}
