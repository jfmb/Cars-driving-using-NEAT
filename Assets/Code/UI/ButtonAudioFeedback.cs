using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ButtonAudioFeedback : MonoBehaviour, ISelectHandler
{
    [SerializeField] private AudioClip feedbackClip;

    private Button _button;
    private AudioSource _audioSource;
    private bool _buttonWasInteractableBeforeClick;
    private int _clickLockFrame = -1;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _audioSource = GetComponentInParent<AudioSource>();

        Assert.IsNotNull(_button, "Button component must be on the same GameObject");
        Assert.IsNotNull(_audioSource, "An AudioSource must exist on a parent GameObject");
        Assert.IsNotNull(feedbackClip, "feedbackClip must be != null");

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        if (_clickLockFrame >= 0 && Time.frameCount > _clickLockFrame)
        {
            RestoreButtonInteractability();
        }

        _button.onClick.AddListener(PlayClickFeedback);
    }

    private void RestoreButtonInteractability()
    {
        if (_clickLockFrame < 0)
        {
            return;
        }

        if (_button != null)
        {
            _button.interactable = _buttonWasInteractableBeforeClick;
        }

        _clickLockFrame = -1;
    }

    private void PlayClickFeedback()
    {
        if (_clickLockFrame == Time.frameCount)
        {
            return;
        }

        // Persistent UnityEvent actions run before this listener; block another UI press until this frame's UI processing ends.
        _buttonWasInteractableBeforeClick = _button.interactable;
        _button.interactable = false;
        _clickLockFrame = Time.frameCount;
        PlayFeedback();
    }

    private void PlayFeedback()
    {
        _audioSource.PlayOneShot(feedbackClip);
    }

    private void LateUpdate()
    {
        RestoreButtonInteractability();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (eventData is PointerEventData pointerEventData
            && pointerEventData.button == PointerEventData.InputButton.Left
            && pointerEventData.eligibleForClick)
        {
            return;
        }

        PlayFeedback();
    }

    private void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(PlayClickFeedback);
        }

        if (gameObject.activeInHierarchy)
        {
            RestoreButtonInteractability();
        }
    }
}
