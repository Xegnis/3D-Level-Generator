using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StixGames.TileComposer
{
    public class TileVariation : IEquatable<TileVariation>
    {
        public readonly string EmptyName;
        public readonly Tile Tile;
        public readonly int TileType;
        public readonly NeighborCompatibiltyMatrix[] Neighbors;
        public readonly Quaternion Rotation;
        public readonly int[] OriginalSides;
        public double VariationWeight;

        public string TileTypeName => Tile == null ? EmptyName : Tile.GetOverridenTileType() ?? EmptyName;

        private readonly int hashCode;
        
        // Empty Type constructor
        public TileVariation(int tileType, string emptyName, int sides, string[] allTypes, bool[] isEmptyType,
            Quaternion rotation, double weight)
        {
            EmptyName = emptyName;
            Tile = null;
            TileType = tileType;
            Neighbors = new NeighborCompatibiltyMatrix[allTypes.Length];
            for (var i = 0; i < Neighbors.Length; i++)
            {
                Neighbors[i] = new NeighborCompatibiltyMatrix(allTypes[i], sides);
                for (var j = 0; j < Neighbors[i].Sides.Length; j++)
                {
                    if (isEmptyType[i] && tileType != i)
                    {
                        // All empty types do not support each other, only themselves
                        Neighbors[i].Sides[j] = 0;
                    }
                    else
                    {
                        // But they support all other types of tiles, so the tiles can define the support
                        Neighbors[i].Sides[j] = ~0;
                    }
                }
            }

            Rotation = rotation;
            OriginalSides = Enumerable.Range(0, sides).ToArray();
            VariationWeight = weight;

            hashCode = CalculateHashCode();
        }

        // Regular tile constructors
        public TileVariation(int tileType, Tile tile, int sides, NeighborCompatibiltyMatrix[] neighbors)
            : this(tileType, tile, neighbors, Quaternion.identity, Enumerable.Range(0, sides).ToArray())
        {
        }

        public TileVariation(int tileType, Tile tile, NeighborCompatibiltyMatrix[] neighbors, Quaternion rotation,
            int[] originalSides)
        {
            EmptyName = null;
            Tile = tile;
            TileType = tileType;
            Neighbors = neighbors;
            Rotation = rotation;
            OriginalSides = originalSides;
            
            hashCode = CalculateHashCode();
        }

        public TileVariation CreateRotated(IGrid grid, int axis, int i)
        {
            var neighbors = RotateNeighbors(grid, axis, i);
            var rotation = grid.GetRotation(axis, i) * Rotation;
            var originalSides = grid.RotateSideArray(OriginalSides, axis, i);

            return new TileVariation(TileType, Tile, neighbors, rotation, originalSides);
        }

        private NeighborCompatibiltyMatrix[] RotateNeighbors(IGrid grid, int axis, int i)
        {
            return Neighbors.Select(neighbor => neighbor.RotateSides(grid, axis, i)).ToArray();
        }

        public bool Equals(TileVariation other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return EmptyName == other.EmptyName && Equals(Tile, other.Tile) &&
                   OriginalSides.SequenceEqual(other.OriginalSides);
        }

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

            return Equals((TileVariation) obj);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        private int CalculateHashCode()
        {
            int hashCode;
            unchecked
            {
                hashCode = (EmptyName != null ? EmptyName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Tile != null ? Tile.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (OriginalSides != null
                               ? ((IStructuralEquatable) OriginalSides).GetHashCode(EqualityComparer<int>.Default)
                               : 0);
            }

            return hashCode;
        }

        public override string ToString()
        {
            return
                $"{nameof(Tile)}: {Tile?.name ?? EmptyName}, {nameof(Rotation)}: {Rotation.eulerAngles}, {nameof(TileType)}: {TileType}";
        }
    }
}