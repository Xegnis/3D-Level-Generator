using System;
using UnityEngine;

namespace StixGames.TileComposer
{
    [Serializable]
    public class SATSettings
    {
        [Tooltip(
            "When enabled, the constraints for this model will be randomly shuffled. This removes certain artifacts from the solver, but in theory it could cause some slowdowns for certain models. In general you can leave this enabled.")]
        public bool RandomizeConstraintOrder = true;

        [Range(0.0f, 1.0f)]
        [Tooltip(
            "The Z3 Solver only uses logical formulas, so you can't simply increase the weight / probability of a certain tile. Instead you can enforce rules.\n\n" +
            "Weight priory enforces that rations between tiles is approximately the same as the ratio of their weights. This is a very vague constraint and you should probably use Percentage and Absolute constraints instead." +
            "\n\nIf you increase it, the calculation time will increase and it is more likely that the algorithm will fail to produce a result (e.g. it will be impossible to fulfill the constraints)")]
        public float WeightPriority = 0.0f;

        [Tooltip(
            "Allows you to make constraints for the amount of each tile type in the model. For example you could restrict the empty space between buildings, to create a denser model.")]
        public PercentageConstraint[] PercentageConstraints;

        [Tooltip(
            "Allows you to make constraints for the amount of each tile type in the model, with absolute values. For example you could say that a building should have at least 1 door, but no more than 2.")]
        public AbsoluteConstraint[] AbsoluteConstraints;
        
        [Tooltip("Create custom functionality by restricting the sum of a custom value. Be careful, this condition will add a large amount of conditions, which likely increases performance cost.")]
        public AbsoluteConstraint[] CustomPropertyConstraints;
    }
}