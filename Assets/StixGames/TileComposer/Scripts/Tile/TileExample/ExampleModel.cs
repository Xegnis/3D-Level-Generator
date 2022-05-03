using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace StixGames.TileComposer
{
    [AddComponentMenu("Stix Games/Tile Composer/Example Models/Example Model")]
    public class ExampleModel : MonoBehaviour
    {
        [Tooltip("The example model will use tiles from this collection.")]
        public TileCollection TileCollection;

        public int[] GridSize;

        public IGrid GetGrid() => TileCollection.GetGrid(GridSize);

        public int GetIndex(Vector3 localPosition)
        {
            var grid = GetGrid();

            var coords = grid.CalculateIndexOffset(Vector3.zero, localPosition);
            return grid.SliceToIndices(coords.Select(x => new Slice(x, x)).ToArray(), false).Single();
        }

        public (int, int) GetRotation(int index, Quaternion localRotation)
        {
            var grid = GetGrid();

            var minDifference = float.MaxValue;
            var rotation = (-1, -1);
            for (var axis = 0; axis < grid.RotationAxes.Length; axis++)
            {
                for (var step = 0; step < grid.RotationSteps(axis); step++)
                {
                    var possibleRotation = grid.GetTileRotation(index) * grid.GetRotation(axis, step);

                    var difference = Quaternion.Angle(possibleRotation, localRotation);
                    if (difference < minDifference)
                    {
                        minDifference = difference;
                        rotation = (axis, step);
                    }
                }
            }

            Assert.IsTrue(rotation.Item1 >= 0 && rotation.Item2 >= 0);

            return rotation;
        }

        /// <summary>
        /// Returns tuples of (TypeName, OriginalSides) for the example model
        /// </summary>
        /// <returns></returns>
        [NotNull]
        public (string, int[])?[] GetExampleModel()
        {
            var grid = GetGrid();

            var array = new (string, int[])?[grid.GridSize];
            var tiles = GetComponentsInChildren<ExampleTile>();

            var sideArray = Enumerable.Range(0, grid.Sides).ToArray();

            foreach (var tile in tiles)
            {
                var t = tile.transform;

                var index = GetIndex(t.localPosition);

                // Ignore tiles outside of grid
                if (index < 0 || grid.GridSize <= index)
                {
                    continue;
                }
                
                var (axis, steps) = GetRotation(index, t.localRotation);

                if (array[index] != null)
                {
                    Debug.LogWarning($"One of the example grid points contains at least two tiles: " +
                                     $"{array[index].Value.Item1} and {tile.TileType}", tile);
                }
                
                array[index] = (tile.TileType, grid.RotateSideArray(sideArray, axis, steps));
            }

            return array;
        }
    }
}