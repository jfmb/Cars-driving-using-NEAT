using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class SelectableNavigationGroup : MonoBehaviour
{
    [SerializeField] private Selectable[] selectables;
    [SerializeField] private Selectable initialSelection;
    [SerializeField] private bool wrapVerticalNavigation;
    [SerializeField] private Color selectedColor = new Color(1f, 0.68f, 0.2f, 1f);

    private InputAction _navigationAction;
    private GameObject _cachedSelectedObject;
    private Selectable _cachedSelectedSelectable;
    private bool _navigationRecoveryPending;

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyNavigationSettings();
    }
#endif

    private void ApplyNavigationSettings()
    {
        if (selectables == null)
        {
            return;
        }

        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable == null)
            {
                continue;
            }

            Navigation navigation = selectable.navigation;
            navigation.mode = wrapVerticalNavigation ? Navigation.Mode.Vertical : Navigation.Mode.Automatic;
            navigation.wrapAround = wrapVerticalNavigation;
            if (wrapVerticalNavigation)
            {
                navigation.selectOnUp = null;
                navigation.selectOnDown = null;
                navigation.selectOnLeft = null;
                navigation.selectOnRight = null;
            }
            selectable.navigation = navigation;

            ColorBlock colors = selectable.colors;
            colors.selectedColor = selectedColor;
            selectable.colors = colors;
#if UNITY_EDITOR
            EditorUtility.SetDirty(selectable);
#endif
        }
    }

    private void Awake()
    {
        ApplyNavigationSettings();
    }

    private void OnEnable()
    {
        ApplyNavigationSettings();
        SubscribeToNavigationInput();
    }

    private void SubscribeToNavigationInput()
    {
        if (_navigationAction != null)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null || inputModule.move == null || inputModule.move.action == null)
        {
            return;
        }

        _navigationAction = inputModule.move.action;
        _navigationAction.performed += OnNavigationPerformed;
    }

    private void OnNavigationPerformed(InputAction.CallbackContext context)
    {
        if (context.control == null || !IsSupportedNavigationDevice(context.control.device))
        {
            return;
        }

        if (context.ReadValue<Vector2>().sqrMagnitude <= 0.0001f || HasValidEventSystemSelection())
        {
            return;
        }

        // Defer selection until LateUpdate so this same input is not applied twice.
        _navigationRecoveryPending = true;
    }

    private static bool IsSupportedNavigationDevice(InputDevice device)
    {
        return device is Keyboard || device is Gamepad;
    }

    private bool HasValidEventSystemSelection()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        GameObject selectedObject = eventSystem.currentSelectedGameObject;
        if (selectedObject == null)
        {
            _cachedSelectedObject = null;
            _cachedSelectedSelectable = null;
            return false;
        }

        if (_cachedSelectedObject != selectedObject)
        {
            _cachedSelectedObject = selectedObject;
            _cachedSelectedSelectable = selectedObject.GetComponent<Selectable>();
        }

        return IsAvailable(_cachedSelectedSelectable);
    }

    private static bool IsAvailable(Selectable selectable)
    {
        return selectable != null
            && selectable.isActiveAndEnabled
            && selectable.gameObject.activeInHierarchy
            && selectable.IsInteractable();
    }

    private void Start()
    {
        SubscribeToNavigationInput();
    }

    private void LateUpdate()
    {
        if (!_navigationRecoveryPending)
        {
            return;
        }

        _navigationRecoveryPending = false;
        if (!HasValidEventSystemSelection())
        {
            // The UI module processes Navigate during Update; recover after it has consumed the move with no selection.
            SelectFirstAvailable();
        }
    }

    private void SelectFirstAvailable()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        Selectable firstAvailable = FindFirstAvailable();
        eventSystem.SetSelectedGameObject(IsAvailable(firstAvailable) ? firstAvailable.gameObject : null);
    }

    private Selectable FindFirstAvailable()
    {
        if (selectables == null)
        {
            return null;
        }

        for (int i = 0; i < selectables.Length; i++)
        {
            if (IsAvailable(selectables[i]))
            {
                return selectables[i];
            }
        }

        return null;
    }

    public GameObject CurrentSelection
    {
        get
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return null;
            }

            GameObject selectedObject = eventSystem.currentSelectedGameObject;
            return IsAvailable(FindSelectable(selectedObject)) ? selectedObject : null;
        }
    }

    public void Select(GameObject preferredSelection)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        Selectable selection = FindSelectable(preferredSelection);
        if (!IsAvailable(selection))
        {
            selection = FindSelectable(initialSelection != null ? initialSelection.gameObject : null);
        }

        if (!IsAvailable(selection))
        {
            selection = FindFirstAvailable();
        }

        eventSystem.SetSelectedGameObject(IsAvailable(selection) ? selection.gameObject : null);
    }

    private Selectable FindSelectable(GameObject target)
    {
        if (target == null || selectables == null)
        {
            return null;
        }

        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable != null && selectable.gameObject == target)
            {
                return selectable;
            }
        }

        return null;
    }

    private void OnDisable()
    {
        if (_navigationAction != null)
        {
            _navigationAction.performed -= OnNavigationPerformed;
            _navigationAction = null;
        }

        _navigationRecoveryPending = false;
        _cachedSelectedObject = null;
        _cachedSelectedSelectable = null;
    }
}
