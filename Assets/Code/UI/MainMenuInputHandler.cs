using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class MainMenuInputHandler : MonoBehaviour
{
    [SerializeField] private MainMenuController mainMenuController;
    [SerializeField] private InputSystemUIInputModule inputModule;
    [SerializeField] private TMP_Dropdown settingsDropdown;

    private InputAction _cancelAction;

    private void Awake()
    {
        Assert.IsNotNull(mainMenuController, "mainMenuController must be != null");
        Assert.IsNotNull(inputModule, "inputModule must be != null");

        if (inputModule.cancel == null || inputModule.cancel.action == null)
        {
            Debug.LogError("MainMenuInputHandler requires an InputSystemUIInputModule with a Cancel action.", this);
            return;
        }

        _cancelAction = inputModule.cancel.action;
    }

    private void OnEnable()
    {
        if (_cancelAction != null)
        {
            _cancelAction.performed += OnCancelInputPerformed;
        }
    }

    private void OnCancelInputPerformed(InputAction.CallbackContext context)
    {
        if (!mainMenuController.isActiveAndEnabled)
        {
            return;
        }

        if (mainMenuController.IsConfirmationMenuOpen)
        {
            mainMenuController.ShowMainMenu();
            return;
        }

        if (!mainMenuController.IsSettingsMenuOpen)
        {
            return;
        }

        if (settingsDropdown != null && settingsDropdown.IsExpanded)
        {
            settingsDropdown.Hide();
            return;
        }

        mainMenuController.ShowMainMenu();
    }

    private void OnDisable()
    {
        if (_cancelAction != null)
        {
            _cancelAction.performed -= OnCancelInputPerformed;
        }
    }
}
