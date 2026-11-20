using UnityEngine;

public class FadeStarter : FeatureStarter
{
    [SerializeField] private FadeConsumer fadeConsumer;

    public override void StartFeature()
    {
        fadeConsumer.ExecuteSecondaryInteraction();
    }
}
