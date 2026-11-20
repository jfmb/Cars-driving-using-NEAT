using System;
using System.Collections.Generic;
using System.Globalization;
using Services.EventQueue;
using Services.EventQueue.Classes.EventData;
using Services.EventQueue.Events.ScriptableObjects;
using Services.Languages;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    private const string MasterVolumeParameter = "MasterVolume";
    private const float MinimumVolumeDecibels = -80f;

    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider cameraSensitivitySlider;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private EventId localizationEventId;
    [SerializeField] private EventId cameraSensitivityEventId;

    private readonly List<string> _languageCodes = new();
    private IPermanentData _permanentData;
    private Languages _languages;
    private SettingsService _settingsService;
    private bool _initialized;
    private float _cameraSensitivity;

    private void OnEnable()
    {
        if (!_initialized)
        {
            return;
        }
        RefreshControls();
    }

    private void RefreshControls()
    {
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        vSyncToggle.SetIsOnWithoutNotify(QualitySettings.vSyncCount > 0);
        RefreshMasterVolume();
        cameraSensitivitySlider.SetValueWithoutNotify(_cameraSensitivity);
    }

    private void RefreshMasterVolume()
    {
        if (!audioMixer.GetFloat(MasterVolumeParameter, out var decibels))
        {
            Debug.LogError($"SettingsMenuController: AudioMixer parameter " +
                           $"'{MasterVolumeParameter}' is not exposed.");
            masterVolumeSlider.interactable = false;
            return;
        }

        masterVolumeSlider.interactable = true;
        var normalizedVolume = decibels <= MinimumVolumeDecibels
            ? 0f
            : Mathf.Clamp01(Mathf.Pow(10f, decibels / 20f));
        masterVolumeSlider.SetValueWithoutNotify(normalizedVolume);
    }

    public void InitializeSettings()
    {
        if (_initialized)
        {
            return;
        }

        _permanentData = ServiceLocator.GetService<IPermanentData>();
        _languages = ServiceLocator.GetService<Languages>();
        _settingsService = ServiceLocator.GetService<SettingsService>();
        LoadPersistedSettings();
        RefreshLanguageOptions();
        _initialized = true;
        RefreshControls();
    }

    private void LoadPersistedSettings()
    {
        _permanentData.LoadSingleData(GetKey(AllSettingsKeys.Settings_Language_CurrentLanguage),
            out string savedLanguage);
        if (savedLanguage == null)
        {
            ApplyLanguage(_settingsService.DefaultCurrentLanguage);
        }
        else if (_languages.LanguagesInGame.ContainsKey(savedLanguage))
        {
            ApplyLanguage(savedLanguage);
        }
        else
        {
            Debug.LogWarning($"SettingsMenuController: saved language '{savedLanguage}' is not supported; " +
                             "using the Inspector default.");
            ApplyLanguage(_settingsService.DefaultCurrentLanguage);
        }

        _permanentData.LoadSingleData(GetKey(AllSettingsKeys.Settings_Graphics_Fullscreen),
            out string savedFullscreen);
        if (savedFullscreen == null)
        {
            Screen.fullScreen = _settingsService.DefaultFullscreen;
        }
        else if (bool.TryParse(savedFullscreen, out var isFullscreen))
        {
            Screen.fullScreen = isFullscreen;
        }
        else
        {
            Debug.LogWarning("SettingsMenuController: saved fullscreen setting is invalid; " +
                             "using the Inspector default.");
            Screen.fullScreen = _settingsService.DefaultFullscreen;
        }

        _permanentData.LoadSingleData(GetKey(AllSettingsKeys.Settings_Graphics_VSync), out string savedVSync);
        if (savedVSync == null)
        {
            QualitySettings.vSyncCount = _settingsService.DefaultVSync ? 1 : 0;
        }
        else if (bool.TryParse(savedVSync, out var vSyncEnabled))
        {
            QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
        }
        else
        {
            Debug.LogWarning("SettingsMenuController: saved VSync setting is invalid; " +
                             "using the Inspector default.");
            QualitySettings.vSyncCount = _settingsService.DefaultVSync ? 1 : 0;
        }

        _permanentData.LoadSingleData(GetKey(AllSettingsKeys.Settings_Audio_Volume), out string savedVolume);
        if (savedVolume == null)
        {
            ApplyMasterVolume(_settingsService.DefaultMasterVolume);
        }
        else if (float.TryParse(savedVolume, NumberStyles.Float, CultureInfo.InvariantCulture, out var volume)
            && !float.IsNaN(volume)
            && !float.IsInfinity(volume))
        {
            ApplyMasterVolume(volume);
        }
        else
        {
            Debug.LogWarning("SettingsMenuController: saved master volume is invalid; " +
                             "using the Inspector default.");
            ApplyMasterVolume(_settingsService.DefaultMasterVolume);
        }

        LoadCameraSensitivity();
    }

    private static string GetKey(AllSettingsKeys key)
    {
        return key.ToString();
    }

    private void ApplyLanguage(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode) || !_languages.LanguagesInGame.ContainsKey(languageCode))
        {
            Debug.LogWarning($"SettingsMenuController: default language '{languageCode}' is unsupported; " +
                             "using the language service default.");
            languageCode = _languages.TwoLettersCodeForDefaultLanguage;
        }

        if (_languages.TwoLettersCodeForCurrentLanguage == languageCode)
        {
            return;
        }

        _languages.SetCurrentLanguage(languageCode);
        ServiceLocator.GetService<EventQueue>().EnqueueEvent(localizationEventId, EventArgs.Empty);
    }

    private bool ApplyMasterVolume(float normalizedVolume)
    {
        var volume = Mathf.Clamp01(normalizedVolume);
        var decibels = volume <= 0f
            ? MinimumVolumeDecibels
            : Mathf.Max(MinimumVolumeDecibels, 20f * Mathf.Log10(volume));

        if (audioMixer.SetFloat(MasterVolumeParameter, decibels))
        {
            return true;
        }

        Debug.LogError($"SettingsMenuController: AudioMixer parameter " +
                       $"'{MasterVolumeParameter}' is not exposed.");
        return false;
    }

    private void LoadCameraSensitivity()
    {
        _permanentData.LoadSingleData(GetKey(AllSettingsKeys.Settings_Controls_CameraSensitivity),
            out string savedSensitivity);
        if (string.IsNullOrEmpty(savedSensitivity))
        {
            _cameraSensitivity = _settingsService.DefaultCameraSensitivity;
            return;
        }

        if (float.TryParse(savedSensitivity, NumberStyles.Float, CultureInfo.InvariantCulture, out float sensitivity)
            && !float.IsNaN(sensitivity)
            && !float.IsInfinity(sensitivity))
        {
            _cameraSensitivity = Mathf.Clamp(
                sensitivity,
                SettingsService.MinimumCameraSensitivity,
                SettingsService.MaximumCameraSensitivity);
            return;
        }

        Debug.LogWarning("SettingsMenuController: saved camera sensitivity is invalid; " +
                         "using the Inspector default.");
        _cameraSensitivity = _settingsService.DefaultCameraSensitivity;
    }

    private void RefreshLanguageOptions()
    {
        var options = new List<TMP_Dropdown.OptionData>(_languages.LanguagesInGame.Count);
        _languageCodes.Clear();

        foreach (var language in _languages.LanguagesInGame)
        {
            _languageCodes.Add(language.Key);
            options.Add(new TMP_Dropdown.OptionData(language.Value.NativeName));
        }

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(options);

        var selectedIndex = _languageCodes.IndexOf(_languages.TwoLettersCodeForCurrentLanguage);
        if (selectedIndex < 0 && _languageCodes.Count > 0)
        {
            selectedIndex = 0;
        }

        languageDropdown.SetValueWithoutNotify(selectedIndex);
        languageDropdown.RefreshShownValue();
    }

    public void SelectLanguage(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= _languageCodes.Count)
        {
            return;
        }

        var languageCode = _languageCodes[selectedIndex];
        if (_languages.TwoLettersCodeForCurrentLanguage == languageCode)
        {
            return;
        }

        _languages.SetCurrentLanguage(languageCode);
        ServiceLocator.GetService<EventQueue>().EnqueueEvent(localizationEventId, EventArgs.Empty);
        _permanentData.SaveSingleData(GetKey(AllSettingsKeys.Settings_Language_CurrentLanguage), languageCode);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        _permanentData.SaveSingleData(GetKey(AllSettingsKeys.Settings_Graphics_Fullscreen),
            isFullscreen.ToString());
    }

    public void SetVSync(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        _permanentData.SaveSingleData(GetKey(AllSettingsKeys.Settings_Graphics_VSync), enabled.ToString());
    }

    public void SetMasterVolume(float normalizedVolume)
    {
        var volume = Mathf.Clamp01(normalizedVolume);
        if (!ApplyMasterVolume(volume))
        {
            return;
        }

        var savedVolume = volume.ToString("R", CultureInfo.InvariantCulture);
        _permanentData.SaveSingleData(GetKey(AllSettingsKeys.Settings_Audio_Volume), savedVolume);
    }

    public void SetCameraSensitivity(float sensitivity)
    {
        _cameraSensitivity = Mathf.Clamp(
            sensitivity,
            SettingsService.MinimumCameraSensitivity,
            SettingsService.MaximumCameraSensitivity);
        cameraSensitivitySlider.SetValueWithoutNotify(_cameraSensitivity);

        string savedSensitivity = _cameraSensitivity.ToString("R", CultureInfo.InvariantCulture);
        _permanentData.SaveSingleData(GetKey(AllSettingsKeys.Settings_Controls_CameraSensitivity), savedSensitivity);
        ServiceLocator.GetService<EventQueue>().EnqueueEvent(
            cameraSensitivityEventId,
            new FloatEventData(_cameraSensitivity));
    }

    public void RestoreDefaults()
    {
        ApplyLanguage(_settingsService.DefaultCurrentLanguage);
        _permanentData.SaveSingleData(GetKey(AllSettingsKeys.Settings_Language_CurrentLanguage),
            _languages.TwoLettersCodeForCurrentLanguage);
        SetFullscreen(_settingsService.DefaultFullscreen);
        SetVSync(_settingsService.DefaultVSync);
        SetMasterVolume(_settingsService.DefaultMasterVolume);
        SetCameraSensitivity(_settingsService.DefaultCameraSensitivity);
        RefreshLanguageOptions();
        RefreshControls();
    }
}
