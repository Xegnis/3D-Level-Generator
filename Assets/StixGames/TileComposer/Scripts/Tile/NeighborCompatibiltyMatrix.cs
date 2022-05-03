using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StixGames.TileComposer
{
    [Serializable]
    public class NeighborCompatibiltyMatrix
    {
        public string TileType;

        /// <summary>
        /// Each neighbor has a bitmask, to mask out specific sides
        /// </summary>
        public int[] Sides;

        public bool[] Overrides;

        public NeighborCompatibiltyMatrix(string tileType, int sides)
        {
            TileType = tileType;
            Sides = new int[sides];
            Overrides = new bool[sides];
        }

        public NeighborCompatibiltyMatrix(string tileType, int[] sides)
        {
            TileType = tileType;
            Sides = sides;
            Overrides = new bool[sides.Length];
        }

        public NeighborCompatibiltyMatrix(string tileType, int[] sides, bool[] overrides)
        {
            TileType = tileType;
            Sides = sides;
            Overrides = overrides;
        }

        /// <summary>
        /// Checks if the neighbor's original side is supported at this connection
        /// </summary>
        /// <param name="side"></param>
        /// <param name="neighborsOriginalSide"></param>
        /// <returns></returns>
        public bool SupportsNeighborSide(int side, int neighborsOriginalSide)
        {
            return (Sides[side] & (1 << neighborsOriginalSide)) != 0;
        }

        public void SetNeighborSideSupport(int side, int neighborsOriginalSide, bool value)
        {
            if (value)
            {
                Sides[side] |= (1 << neighborsOriginalSide);
            }
            else
            {
                Sides[side] &= ~(1 << neighborsOriginalSide);
            }
        }

        public bool[] GetSideArray(int side, int length)
        {
            return MaskToBoolArray(Sides[side], length);
        }

        public void SetSideArray(int side, bool[] array)
        {
            Sides[side] = BoolArrayToMask(array);
        }
        
        private static bool[] MaskToBoolArray(int mask, int length)
        {
            bool[] array = new bool[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = (mask & (1 << i)) != 0;
            }

            return array;
        }

        private static int BoolArrayToMask(bool[] array)
        {
            int mask = 0;
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i])
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        public NeighborCompatibiltyMatrix RotateSides(IGrid grid, int axis, int i)
        {
            return new NeighborCompatibiltyMatrix(TileType, grid.RotateSideArray(Sides, axis, i),
                grid.RotateSideArray(Overrides, axis, i));
        }

        public bool Equals(NeighborCompatibiltyMatrix other)
        {
            return Sides.SequenceEqual(other.Sides) && Overrides.SequenceEqual(other.Overrides);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is NeighborCompatibiltyMatrix && Equals((NeighborCompatibiltyMatrix) obj);
        }

        public override int GetHashCode()
        {
            // TODO: Make an efficient hash, if necessary
            return 0;
        }

        public void SetSideCount(int sides)
        {
            if (Sides.Length != sides)
            {
                Array.Resize(ref Sides, sides);
            }

            if (Overrides.Length != sides)
            {
                Array.Resize(ref Overrides, sides);
            }
        }
    }
}