using System.Collections;
using Services.EventQueue;
using Services.EventQueue.Classes;
using Services.EventQueue.Classes.EventData;
using Services.EventQueue.Events.ScriptableObjects;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class Fade : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private EventId fadeInEventId;
    [SerializeField] private EventId fadeOutEventId;
    [SerializeField] private bool isFadeOutAtAwake;

    private FloatEvent _fadeInEvent;
    private FloatEvent _fadeOutEvent;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        Assert.IsNotNull(fadeImage, "fadeImage must be != null");
        Assert.IsNotNull(fadeInEventId, "fadeInEventId must be != null");
        Assert.IsNotNull(fadeOutEventId, "fadeOutEventId must be != null");

        if (!isFadeOutAtAwake)
        {
            return;
        }
        
        SetAlpha(1);
        
    }

    public void SetAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    private void OnEnable()
    {
        SubscribeToFadeEvents();
    }

    private void SubscribeToFadeEvents()
    {
        var eventQueue = ServiceLocator.GetService<EventQueue>();
        var fadeInEvent = (FloatEvent)eventQueue.GetEventWithEventId(fadeInEventId);
        fadeInEvent.FloatEventSender += OnFadeIn;
        
        var fadeOutEvent = (FloatEvent)eventQueue.GetEventWithEventId(fadeOutEventId);
        fadeOutEvent.FloatEventSender += OnFadeOut;
    }

    private void OnFadeIn(object source, FloatEventData eventData)
    {
        StartFade(0f, eventData.Value);
    }

    private void OnFadeOut(object source, FloatEventData eventData)
    {
        StartFade(1f, eventData.Value);
    }

    private void StartFade(float targetAlpha, float duration)
    {
        StopFade();

        if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration))
        {
            SetAlpha(targetAlpha);
            return;
        }

        _fadeCoroutine = StartCoroutine(FadeTo(targetAlpha, duration));
    }

    private void StopFade()
    {
        if (_fadeCoroutine == null)
        {
            return;
        }

        StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Keep UI fades running while the pause menu sets Time.timeScale to zero.
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }

        SetAlpha(targetAlpha);
        _fadeCoroutine = null;
    }

    private void OnDisable()
    {
        StopFade();

        EventQueue eventQueue = ServiceLocator.GetService<EventQueue>();
        var fadeInEvent = (FloatEvent)eventQueue.GetEventWithEventId(fadeInEventId);
        fadeInEvent.FloatEventSender -= OnFadeIn;
        
        
        var fadeOutEvent = (FloatEvent)eventQueue.GetEventWithEventId(fadeOutEventId);
        fadeOutEvent.FloatEventSender -= OnFadeOut;
    }
}
