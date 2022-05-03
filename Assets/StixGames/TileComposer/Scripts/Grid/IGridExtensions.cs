using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace StixGames.TileComposer
{
    public static class GridExtensions
    {
        public static int CoordToIndex(this IGrid grid, int[] coords)
        {
            // TODO: This should probably be implemented directly in the IGrid for speedup
            if (coords.Length != grid.Axes)
            {
                throw new ArgumentException("coords didn't have the right amount of axes");
            }
            
            var result = grid.SliceToIndices(coords.Select(x => new Slice(x, x)).ToArray(), false).Single();
            
            // Check if the result is actually the right one and wasn't outside the grid
            var isSameCoord = grid.IndexToCoordinates(result).Zip(coords, (x, y) => x == y).All(x => x);
            if (isSameCoord)
            {
                return result;
            }
            else
            {
                return -1;
            }
        }
        
        public static Vector3 MirrorVector(this IGrid grid, int side, Vector3 vector)
        {
            var normal = grid.GetSideNormal(side);

            // A plane through the center
            var plane = new Plane(normal, 0);
            var centerPoint = plane.ClosestPointOnPlane(vector);

            return centerPoint + (centerPoint - vector);
        }

        public static Ray GetBorderRay(this IGrid grid, int side)
        {
            return new Ray(grid.GetSideCenter(side), grid.GetSideNormal(side));
        }

        public static Plane GetBorderPlane(this IGrid grid, int side)
        {
            return new Plane(grid.GetSideNormal(side), grid.GetSideCenter(side));
        }
        
        public static Plane GetBorderPlane(this IGrid grid, int side, Vector3 positionOffset)
        {
            return new Plane(grid.GetSideNormal(side), grid.GetSideCenter(side) + positionOffset);
        }

        public static Vector3[] GetTileFrame(this IGrid grid)
        {
            var slices = new Slice[grid.Axes];

            for (var i = 0; i < slices.Length; i++)
            {
                slices[i] = new Slice(0, 0);
            }

            return grid.GetSliceFrame(slices);
        }

        public static Vector3[] GetModelFrame(this IGrid grid)
        {
            var slices = new Slice[grid.Axes];

            for (var i = 0; i < slices.Length; i++)
            {
                slices[i] = new Slice(0, -1);
            }

            return grid.GetSliceFrame(slices);
        }
    }
}
