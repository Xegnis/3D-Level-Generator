using System;
using UnityEngine;

namespace StixGames.TileComposer
{
    [Serializable]
    public class WaveFunctionCollapseSettings
    {
        [Header("Failure Reset Area")] 
        [Tooltip("When enabled, the algorithm will try to recover from any conflicts it encounters. If the model is very complex, this can lead to long calculation times. Depending on the model and grid size it might be advisable to use Z3 for extremely complex models instead.")]
        public bool UseFailureRecovery = true;
        
        [Tooltip(
            "The size of the area that will be removed around a failing tile. It's not exactly a radius, but the side length of the n-cube (square in 2D, cube in 3D).")]
        public int FailureResetRadius = 2;

        [Tooltip(
            "Defines after how many errors the reset radius will increase in a certain area. A value of 10 will increase the radius by 1 after 10 errors (disregarding decay).")]
        [Min(0.001f)]
        public float RadiusSizeMultiplier = 1;

        [Space]
        [Tooltip(
            "Before resetting the area around a fault, the solver can roll back the last steps, which can solve propagating errors that would be very hard to resolve otherwise.")]
        public int BacktrackSteps = 2;

        [Tooltip(
            "Defines after how many errors the backtrack step count will increase in a certain area. A value of 10 will increase the radius by 1 after 10 errors (disregarding decay).")]
        [Min(0.001f)]
        public float BacktrackStepsMultiplier = 10;

        [Space]
        [Tooltip(
            "This value defines after how many calculation steps the failure weight will lose half it's weight. The failure decays exponentially, this value is its half life.")]
        [Min(0)]
        public float FailureDecay = 20;

        [Space]
        [Tooltip(
            "In cases where there are failures in the same area, but the decay is too high to reset the relevant tiles, the failure value accelerates slowly. If the multiplier is 10, after 10 failures, failures will have 1 more weight value per failure.")]
        [Min(0)]
        public float FailureAccelerationMultiplier = 10;

        [Tooltip(
            "This value defines after how many calculation steps the failure acceleration will lose half it's weight. The failure decays exponentially, this value is its half life.\n This weight should be higher than the Failure Decay, or it will lose its purpose.")]
        [Min(0)]
        public float FailureAccelerationDecay = 100;
    }
}