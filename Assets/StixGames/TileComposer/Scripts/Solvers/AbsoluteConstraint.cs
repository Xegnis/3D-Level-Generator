using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace StixGames.TileComposer
{
    [Serializable]
    public class AbsoluteConstraint
    {
        [FormerlySerializedAs("TileType")] 
        public string Name;

        /// <summary>
        /// Defines the comparison type
        /// </summary>
        public Comparison Comparison;

        public int Value;
    }
}