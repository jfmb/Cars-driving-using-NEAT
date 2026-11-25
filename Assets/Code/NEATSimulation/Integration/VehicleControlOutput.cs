namespace NeatSimulation.Integration
{
    /// <summary>
    /// Normalized steering and target-speed outputs produced by one NEAT individual.
    /// </summary>
    public readonly struct VehicleControlOutput
    {
        public float Steering { get; }
        public float NormalizedTargetSpeed { get; }

        public VehicleControlOutput(float steering, float normalizedTargetSpeed)
        {
            Steering = steering;
            NormalizedTargetSpeed = normalizedTargetSpeed;
        }
    }
}
