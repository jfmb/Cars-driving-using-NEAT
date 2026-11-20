using UnityEngine;

public class SettingsService : MonoBehaviour
{
    public const float MinimumCameraSensitivity = 0f;
    public const float MaximumCameraSensitivity = 2f;

    [Tooltip("Default normalized Master Volume, from 0 (silent) to 1 (full volume)")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Tooltip("Default fullscreen mode")]
    [SerializeField] private bool fullscreen = true;

    [Tooltip("Default VSync state")]
    [SerializeField] private bool vSync = true;

    [Tooltip("Default language code supported by the localization system")]
    [SerializeField] private string currentLanguage = "en";

    [Tooltip("Default camera sensitivity. Zero disables camera rotation.")]
    [Range(MinimumCameraSensitivity, MaximumCameraSensitivity)]
    [SerializeField] private float cameraSensitivity = 1f;

    public float DefaultMasterVolume => masterVolume;
    public bool DefaultFullscreen => fullscreen;
    public bool DefaultVSync => vSync;
    public string DefaultCurrentLanguage => currentLanguage;
    public float DefaultCameraSensitivity => cameraSensitivity;
}
