using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

namespace StixGames.TileComposer
{
    public interface IGrid
    {
        #region Grid Data
        /// <summary>
        /// The count of all tiles in the grid
        /// </summary>
        int GridSize { get; }
        int Sides { get; }

        string[] SideNames { get; }

        int Axes { get; }
        
        string[] AxisNames { get; }
        
        string[] RotationAxes { get; }
        
        /// <summary>
        /// Returns the number of steps necessary for a full rotation on the selected axis
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        int RotationSteps(int axis);
        
        #endregion
        
        #region Grid Navigation
        
        /// <summary>
        /// Converts a slice of coordinates to grid indices.
        /// </summary>
        /// <param name="slice"></param>
        /// <param name="wrapAround"></param>
        /// <returns></returns>
        int[] SliceToIndices([NotNull] Slice[] slice, bool wrapAround = true);
        
        /// <summary>
        /// Converts a tile index to coordinates
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        int[] IndexToCoordinates(int index);
        
        /// <summary>
        /// Returns the index of a connected grid tile, or -1 if it's outside the grid border
        /// </summary>
        /// <param name="index"></param>
        /// <param name="side"></param>
        /// <returns></returns>
        int GetNeighbor(int index, int side);
        
        /// <summary>
        /// Returns the neighboring side that is touching the parameter side
        /// Can be used to check neighbor compatibility.
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        int GetNeighborSide(int side);
        
        /// <summary>
        /// Takes an array representing each side of a tile and rotates them around <c>axis</c> b <c>steps</c>
        /// </summary>
        /// <param name="array"></param>
        /// <param name="axis"></param>
        /// <param name="steps"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T[] RotateSideArray<T>([NotNull] T[] array, int axis, int steps);
        
        #endregion

        #region Grid to World Functions
        
        /// <summary>
        /// Returns the local position of the tile.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Vector3 GetPosition(int index);
        
        /// <summary>
        /// Returns the local offset of the tile, in case the grid tiles aren't all rotated evenly.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        Quaternion GetTileRotation(int index);

        /// <summary>
        /// Returns a quaternion rotating a tile by steps around axis.
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        Quaternion GetRotation(int axis, int steps);
        
        /// <summary>
        /// Returns the center of a side for a tile without rotation
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        Vector3 GetSideCenter(int side);
        
        /// <summary>
        /// Returns the normal of the slices side
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        Vector3 GetSideNormal(int side);

        /// <summary>
        /// Return an array of points defining the lines of the whole grid.
        /// </summary>
        /// <returns></returns>
        Vector3[] GetSliceFrame([NotNull] Slice[] slice, bool wrapAround = true);

        /// <summary>
        /// Returns the center of the slice
        /// </summary>
        /// <param name="slice"></param>
        /// <param name="side"></param>
        /// <param name="wrapAround"></param>
        /// <returns></returns>
        Vector3 GetSliceCenter([NotNull] Slice[] slice, bool wrapAround = true);

        /// <summary>
        /// Returns the center of the slices border, this doesn't have to correspond to a tile side
        /// </summary>
        /// <param name="side"></param>
        /// <returns></returns>
        Vector3 GetSliceBorderCenter([NotNull] Slice[] slice, int axis, bool isPositive, bool wrapAround = true);

        /// <summary>
        /// Returns the border normal for the current slice
        /// </summary>
        /// <param name="slice"></param>
        /// <param name="axis"></param>
        /// <param name="isPositive"></param>
        /// <param name="wrapAround"></param>
        /// <returns></returns>
        Vector3 GetSliceBorderNormal([NotNull] Slice[] slice, int axis, bool isPositive, bool wrapAround = true);
        
        #endregion

        #region World to Grid

        /// <summary>
        /// Takes a vector in local coordinates and calculates how far in index coordinates the change is
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="localOffset"></param>
        /// <returns></returns>
        int[] CalculateIndexOffset(Vector3 origin, Vector3 localOffset);

        /// <summary>
        /// Converts a local offset value into a slice offset.
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="isPositive"></param>
        /// <param name="offset"></param>
        /// <returns>(start==false/end==true offset, amount)</returns>
        int CalculateSliceOffset(int axis, bool isPositive, Vector3 origin, Vector3 offset);
        
        #endregion
        
        #region Visualization

        /// <summary>
        /// Returns a mesh that has the same shape as a grid tile, which can be used to visualize the grid
        /// </summary>
        /// <returns></returns>
        Mesh GetSlowGenerationMesh();
        
        /// <summary>
        /// Returns true if the given side is visible from the view dir, which is given in the grids local coordinates.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="localViewDir"></param>
        /// <returns></returns>
        bool IsSideVisible(int side, Vector3 localViewDir);

        #endregion
        
        #region WFC Backtracking
        
        /// <summary>
        /// Returns a boolean array that defines which sides are on the border of the slice. [index, side]
        /// </summary>
        /// <param name="slice"></param>
        /// <param name="wrapAround"></param>
        /// <returns></returns>
        bool[,] SliceBorderSides([NotNull] Slice[] slice, bool wrapAround = true);
        
        #endregion
    }
}