using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace StixGames.TileComposer
{
    [Serializable]
    public class PercentageConstraint
    {
        [FormerlySerializedAs("TileType")] public string Name;

        /// <summary>
        /// Defines the constraint type
        /// </summary>
        public Comparison Comparison;

        [Range(0, 1.0f)]
        public float Value;
    }
}