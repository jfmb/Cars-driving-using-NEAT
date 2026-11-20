using TMPro;
using UnityEngine;

public class TextAlphaPulse : MonoBehaviour
{
    [SerializeField] private TMP_Text textToPulse;
    [SerializeField] private float minAlpha = 0.4f;
    [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private float pulseSpeed = 2f;

    private void Update()
    {
        var normalizedPulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        var alpha = Mathf.Lerp(minAlpha, maxAlpha, normalizedPulse);

        var color = textToPulse.color;
        color.a = alpha;
        textToPulse.color = color;
    }
}
