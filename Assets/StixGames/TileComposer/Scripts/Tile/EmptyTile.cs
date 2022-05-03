using System;
using UnityEngine;

namespace StixGames.TileComposer
{
    [Serializable]
    public class EmptyTile
    {
        public string Name = "Empty";
        public float Weight = 1.0f;

        [Tooltip("If the empty type is compressible, it allows tiles that can border the same empty type to " +
                 "border each other, allowing dense structures, where the emptiness is defined in the models, " +
                 "instead of the grid.\n\nThis setting is only relevant if you auto-set the tile neighbors.")]
        public bool IsCompressible;

        public EmptyTile() : this("Empty", 1.0f)
        {
        }

        public EmptyTile(string name, float weight)
        {
            Name = name;
            Weight = weight;
        }
    }
}