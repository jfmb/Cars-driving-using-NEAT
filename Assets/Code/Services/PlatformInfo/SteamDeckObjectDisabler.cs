using UnityEngine;

public class SteamDeckObjectDisabler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!ServiceLocator.GetService<PlatformInfo>().IsPlatformSteamDeck)
        {
            return;
        }
        
        gameObject.SetActive(false);
    }
}
