using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StixGames.TileComposer
{
    [SelectionBase]
    [AddComponentMenu("Stix Games/Tile Composer/Tile")]
    public class Tile : MonoBehaviour
    {
        [Tooltip("Optionally add a base tile for this tile. They will share the same tile type. You can make some variation, by using overrides in the neighbor matrix.")]
        public Tile BaseTile;

        /// <summary>
        /// The name of the current tile type
        /// </summary>
        [Tooltip("The name of your tile type. This will be used to reference the tile in the editor of other tiles. It has to be unique.")]
        public string TileType = "Tile";
        
        /// <summary>
        /// The tile can be rotated along these axes (in this order)
        /// </summary>
        [Tooltip("Rotation axes can be used to create variations of your tile that are rotated around the selected axes. \n" +
                 "The editor will automatically remove identical rotations, so you don't have to be careful about creating too many variations.")]
        public int[] RotationAxes = new int[0];

        /// <summary>
        /// Possible connections for each side
        /// </summary>
        public NeighborCompatibiltyMatrix[] Neighbors;

        /// <summary>
        /// Defines connectors for each side, which can be used to easily make sides with the same shape compatible, without having to manage them individually.
        /// </summary>
        public ConnectorSide[] Connectors;

        /// <summary>
        /// Defines how frequently the tile will occur in the final model.
        /// </summary>
        [Tooltip("The base weight changes the probability for the tile to appear. A higher weight leads to a higher probability of your tile.\n" +
                 "Note that other factors can influence how often it will appear in the model, e.g. if it has no valid neighbors,\n" +
                 "or it is very hard to create a state where this tile is valid, your tile will still not occur, even with a high weight.\n" +
                 "\n" +
                 "Not all solvers use this feature.")]
        [Min(0.0001f)]
        public double BaseWeight = 1.0f;

        /// <summary>
        /// When generating the neighbor matrix automatically, this defines if this tile can
        /// have itself as neighbor.
        /// </summary>
        [Tooltip(
            "When generating the neighbor matrix automatically, or using connectors, this defines if this tile can have itself as neighbor.")]
        public bool CanNeighborSelf = true;

        /// <summary>
        /// The custom properties for this tiles.
        /// </summary>
        public CustomTileProperty[] CustomProperties;

        /// <summary>
        /// Returns the combined connectors of this tile and all its parent tiles.
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        public ConnectorAssignment[] GetCombinedConnectors(int side)
        {
            if (Connectors == null)
            {
                return new ConnectorAssignment[0];
            }

            if (side >= Connectors.Length)
            {
                return new ConnectorAssignment[0];
            }

            return (Connectors[side].Connectors ?? new ConnectorAssignment[0]).Concat(
                BaseTile?.GetCombinedConnectors(side) ?? new ConnectorAssignment[0]).Distinct().ToArray();
        }
        
        public string GetOverridenTileType()
        {
            return BaseTile == null ? TileType : BaseTile.GetOverridenTileType();
        }

        /// <summary>
        /// Returns the neighbor matrix from the parent, with overriden values from this tile.
        /// The returned array is a clone, so it's save to modify its values.
        /// </summary>
        /// <returns></returns>
        public NeighborCompatibiltyMatrix[] GetOverridenNeighborSides()
        {
            if (BaseTile == null)
            {
                // Create a copy
                return Neighbors
                    .Select(x => new NeighborCompatibiltyMatrix(x.TileType, x.Sides.ToArray()))
                    .ToArray();
            }

            var baseNeighbors = BaseTile.GetOverridenNeighborSides();

            var possibleConnections = new List<NeighborCompatibiltyMatrix>();
            for (var neighborIndex = 0; neighborIndex < Neighbors.Length; neighborIndex++)
            {
                var baseNeighbor = baseNeighbors[neighborIndex];
                var neighbors = Neighbors[neighborIndex];

                var sides = new List<int>();
                for (var side = 0; side < neighbors.Sides.Length; side++)
                {
                    sides.Add(neighbors.Overrides[side] ? neighbors.Sides[side] : baseNeighbor.Sides[side]);
                }

                possibleConnections.Add(new NeighborCompatibiltyMatrix(baseNeighbor.TileType, sides.ToArray()));
            }

            return possibleConnections.ToArray();
        }

        protected bool Equals(Tile other) => base.Equals(other) && Equals(BaseTile, other.BaseTile) &&
                                             RotationAxes.SequenceEqual(other.RotationAxes) &&
                                             TileType == other.TileType &&
                                             Neighbors.SequenceEqual(other.Neighbors) &&
                                             BaseWeight.Equals(other.BaseWeight) &&
                                             CanNeighborSelf == other.CanNeighborSelf;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((Tile) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (BaseTile != null ? BaseTile.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TileType != null ? TileType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ BaseWeight.GetHashCode();
                hashCode = (hashCode * 397) ^ CanNeighborSelf.GetHashCode();
                return hashCode;
            }
        }
    }
}