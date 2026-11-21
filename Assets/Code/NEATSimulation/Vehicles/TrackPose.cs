using UnityEngine;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// World pose sampled from a track path: position on the centerline and its forward direction.
    /// </summary>
    public readonly struct TrackPose
    {
        public Vector3 Position { get; }

        public Vector3 Forward { get; }

        public TrackPose(Vector3 position, Vector3 forward)
        {
            Position = position;
            Forward = forward;
        }
    }
}
