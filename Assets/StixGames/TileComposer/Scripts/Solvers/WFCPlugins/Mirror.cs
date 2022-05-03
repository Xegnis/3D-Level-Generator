using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StixGames.TileComposer.Solvers.WFCPlugins
{
    [RequireComponent(typeof(TileComposer))]
    [AddComponentMenu("Stix Games/Tile Composer/WFC Plugins/Mirror")]
    public class Mirror : MonoBehaviour, IWFCPlugin
    {
        public int Axis;

        public int MirrorTile;

        private TileComposerInput input;

        /// <summary>
        /// All variations that are not part of the current tile type
        /// </summary>
        private int[][] otherTileTypes;

        public void Initialize(TileComposerInput input)
        {
            this.input = input;

            // Maybe there should be a better way of finding this count?
            var tileTypes = input.TileVariations.Max(x => x.TileType) + 1;

            otherTileTypes = new int[tileTypes][];

            for (int i = 0; i < tileTypes; i++)
            {
                otherTileTypes[i] = input.TileVariations
                    .Where(x => x.TileType != i)
                    .Select(x => input.TileIndices[x])
                    .ToArray();
            }
        }

        public WFCManipulator Step(bool[,] allowedTiles, IList<WFCChange> lastChanges)
        {
            var manipulator = new WFCManipulator();

            var visited = new HashSet<int>();
            foreach (var change in lastChanges)
            {
                //  Only visit each point once
                if (visited.Contains(change.Index))
                {
                    continue;
                }

                visited.Add(change.Index);
                
                // Ignore backtracking
                if (change.IsAllowed)
                {
                    continue;
                }

                // If there is exactly one tile type here now, block it on the other side too!
                int tileType = -1;
                for (int i = 0; i < allowedTiles.GetLength(1); i++)
                {
                    // Check if the tile type is still allowed
                    if (allowedTiles[change.Index, i])
                    {
                        var indexType = input.TileVariations[i].TileType;
                        
                        // It's the first one
                        if (tileType < 0)
                        {
                            tileType = indexType;
                        } else if (tileType != indexType)
                        {
                            tileType = -1;
                            break;
                        }
                    }
                }

                // The tile has either multiple types or no types, ignore it
                if (tileType < 0)
                {
                    continue;
                }

                // Get mirrored coordinates
                var coords = input.Grid.IndexToCoordinates(change.Index);

                // Mirror the change, then block the mirrored tile as well.
                coords[Axis] = -(coords[Axis] - MirrorTile) + MirrorTile;
                var index = input.Grid.CoordToIndex(coords);

                if (index < 0)
                {
                    continue;
                }
                
                manipulator.BlockTiles(index, otherTileTypes[tileType]);
            }

            return manipulator;
        }
    }
}