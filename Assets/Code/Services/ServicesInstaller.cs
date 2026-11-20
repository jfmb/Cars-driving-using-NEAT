using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum AllScenes
{
    Scene00,
    Scene01_1,
    Scene01,
    Scene02NEATRace
}
namespace Services
{
    public class ServicesInstaller : MonoBehaviour
    {
        [SerializeField] private AllScenes nextScene;
        [SerializeField] private GameService languagesInstaller;
        [Tooltip("Filename stored in Application.persistentDataPath for permanent data")]
        [SerializeField] private string permanentDataFileName = FilePermanentDataAdapter.DefaultFileName;
        
        [SerializeField] private GameService[] otherGameServicesToInstall;
        
        private void Awake()
        {
            InstallPlatformInfo();
            InstallPermanentDataSaver();
            InstallDeviceAdapters();
            InstallGameLanguageService();
        }

        private void InstallPlatformInfo()
        {
            var newPlatformInfo = new PlatformInfo();
            newPlatformInfo.Setup();
            ServiceLocator.RegisterService(newPlatformInfo);
        }

        private void InstallPermanentDataSaver()
        {
            var filePermanentDataAdapter = new FilePermanentDataAdapter(permanentDataFileName);
            ServiceLocator.RegisterService<IPermanentData>(filePermanentDataAdapter);
        }

        private void InstallDeviceAdapters()
        {
            // var specificDeviceServices = new SpecificDeviceService();
            //
            // var remoteSteamAchievementAdapter = new RemoteAchievementsSteamAdapter();
            // specificDeviceServices.InjectRemoteAchievements(remoteSteamAchievementAdapter);
            //
            // ServiceLocator.RegisterService(specificDeviceServices);
        }
        
        private void InstallGameLanguageService()
        {
            StartCoroutine(InstallLanguages());
        }


        private IEnumerator InstallLanguages()
        {
            languagesInstaller.Install();
            
            yield return new WaitUntil(()=>languagesInstaller.IsDone());
            
            InstallServicesFromGameObjects();
        }

        private void InstallServicesFromGameObjects()
        {
            Debug.Log("My debug: localization and languages installed!!!");
            foreach (var newService in otherGameServicesToInstall)
            {
                newService.Install();
            }
            
            Debug.Log("Services installed");
            StartNextScene();  
        }
       
        private void StartNextScene()
        {
//            var args = new IntegerEventData((int)nextScene);
//            ServiceLocator.GetService<EventQueue.EventQueue>().EnqueueEvent(nextSceneEventId, args);
            
            SceneManager.LoadScene((int)nextScene);
        }
    }
}
