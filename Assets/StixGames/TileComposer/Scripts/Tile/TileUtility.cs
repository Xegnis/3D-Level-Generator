using System.Collections.Generic;

namespace StixGames.TileComposer
{
    public class TileUtility
    {
        /// <summary>
        /// Creates a matrix of all tile variations, with an array of their supported neighbors. [tile, side][supported neighbor]
        /// </summary>
        /// <param name="tileVariations"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static int[,][] CreateTileCompatibilityMatrix(TileVariation[] tileVariations, IGrid grid)
        {
            // As we need all other tiles, for a specific side of a specific tile, 
            // we put the other tiles in the last dimension, so cache locality can be used
            var tileCompatibilityMatrix = new List<int>[tileVariations.Length, grid.Sides];

            for (int variation = 0; variation < tileVariations.Length; variation++)
            {
                for (int side = 0; side < grid.Sides; side++)
                {
                    tileCompatibilityMatrix[variation, side] = new List<int>();
                }
            }

            // Iterate all tile variations
            for (var current = 0; current < tileCompatibilityMatrix.GetLength(0); current++)
            {
                var currentTile = tileVariations[current];
                var currentType = currentTile.TileType;

                // And create a list of all indices it can be neighbor of
                // Skip all the variations we've visited already
                for (var other = current; other < tileVariations.Length; other++)
                {
                    var otherTile = tileVariations[other];
                    var otherType = otherTile.TileType;

                    // On every side
                    for (var side = 0; side < tileCompatibilityMatrix.GetLength(1); side++)
                    {
                        var otherSide = grid.GetNeighborSide(side);
                        var currentOriginalSide = currentTile.OriginalSides[side];
                        var otherOriginalSide = otherTile.OriginalSides[otherSide];

                        var currentSupportsOther = currentTile.Neighbors[otherType]
                                                              .SupportsNeighborSide(side, otherOriginalSide);
                        var otherSupportsCurrent = otherTile.Neighbors[currentType]
                                                            .SupportsNeighborSide(otherSide, currentOriginalSide);

                        var isSupported = currentSupportsOther && otherSupportsCurrent;

                        if (isSupported)
                        {
                            tileCompatibilityMatrix[current, side].Add(other);

                            if (current != other)
                            {
                                tileCompatibilityMatrix[other, otherSide].Add(current);
                            }
                        }
                    }
                }
            }

            // Convert the lists to arrays
            var matrix = new int[tileVariations.Length, grid.Sides][]; 
            for (var index = 0; index < tileCompatibilityMatrix.GetLength(0); index++)
            {
                for (var side = 0; side < tileCompatibilityMatrix.GetLength(1); side++)
                {
                    matrix[index, side] = tileCompatibilityMatrix[index, side].ToArray();
                }
            }

            return matrix;
        }
        
        /// <summary>
        /// Creates a lookup table to check if tiles support each other as neighbor. [tile, side, neighbor]
        /// </summary>
        /// <param name="tileVariations"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static bool[,,] CreateTileCompatibilityLookup(TileVariation[] tileVariations, IGrid grid)
        {
            // As we need all other tiles, for a specific side of a specific tile, 
            // we put the other tiles in the last dimension, so cache locality can be used
            var tileCompatibilityMatrix = new bool[tileVariations.Length, grid.Sides, tileVariations.Length];

            // Iterate all tile variations
            for (var current = 0; current < tileCompatibilityMatrix.GetLength(0); current++)
            {
                var currentTile = tileVariations[current];
                var currentType = currentTile.TileType;

                // And create a list of all indices it can be neighbor of
                // Skip all the variations we've visited already
                for (var other = current; other < tileVariations.Length; other++)
                {
                    var otherTile = tileVariations[other];
                    var otherType = otherTile.TileType;

                    // On every side
                    for (var side = 0; side < grid.Sides; side++)
                    {
                        var otherSide = grid.GetNeighborSide(side);
                        var currentOriginalSide = currentTile.OriginalSides[side];
                        var otherOriginalSide = otherTile.OriginalSides[otherSide];

                        var currentSupportsOther = currentTile.Neighbors[otherType]
                                                              .SupportsNeighborSide(side, otherOriginalSide);
                        var otherSupportsCurrent = otherTile.Neighbors[currentType]
                                                            .SupportsNeighborSide(otherSide, currentOriginalSide);

                        var isSupported = currentSupportsOther && otherSupportsCurrent;

                        if (isSupported)
                        {
                            tileCompatibilityMatrix[current, side, other] = true;

                            if (current != other)
                            {
                                tileCompatibilityMatrix[other, otherSide, current] = true;
                            }
                        }
                    }
                }
            }

            return tileCompatibilityMatrix;
        }
    }
}