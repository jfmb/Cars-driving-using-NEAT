using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Dropdown))]
public sealed class SettingsDropdownCancelHandler : MonoBehaviour, ICancelHandler
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private MainMenuController mainMenuController;

    private bool _wasExpandedLastFrame;

    private void Awake()
    {
        if (dropdown == null)
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        if (dropdown != null)
        {
            _wasExpandedLastFrame = dropdown.IsExpanded;
        }
    }

    private void LateUpdate()
    {
        if (dropdown != null)
        {
            _wasExpandedLastFrame = dropdown.IsExpanded;
        }
    }

    public void OnCancel(BaseEventData eventData)
    {
        if (_wasExpandedLastFrame || (dropdown != null && dropdown.IsExpanded))
        {
            return;
        }

        if (mainMenuController == null)
        {
            return;
        }

        eventData.Use();
        mainMenuController.ShowMainMenu();
    }
}
