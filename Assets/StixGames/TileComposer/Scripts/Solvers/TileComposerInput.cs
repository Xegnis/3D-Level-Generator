using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace StixGames.TileComposer
{
    public class TileComposerInput
    {
        public readonly System.Random Random;
        public readonly IGrid Grid;
        
        [NotNull]
        public readonly TileVariation[] TileVariations;
        
        [NotNull]
        public readonly TileVariation[][] BlockedTiles;

        private Dictionary<TileVariation, int> tileIndices;
        public Dictionary<TileVariation, int> TileIndices => tileIndices ?? (tileIndices = GetTileIndices());

        private TileVariation[][] uniqueVariations;

        /// <summary>
        /// An array of variations that share the same neighbor matrices
        ///
        /// Unique indices are used for Z3 and SAT based solvers.
        /// </summary>
        public TileVariation[][] UniqueVariations => uniqueVariations ??
                                                     (uniqueVariations = FindUniqueVariations());

        private int[] uniqueVariationRepresentatives;

        /// <summary>
        /// Lookup from the unique index to any one of the duplicates.
        ///
        /// Unique indices are used for Z3 and SAT based solvers.
        /// </summary>
        public int[] UniqueVariationRepresentatives =>
            uniqueVariationRepresentatives ?? (uniqueVariationRepresentatives = GetUniqueVariationRepresentatives());

        private int[] tileIndexToUniqueIndex;

        /// <summary>
        /// A lookup from the tile type to its unique id
        ///
        /// Unique indices are used for Z3 and SAT based solvers.
        /// </summary>
        public int[] TileIndexToUniqueIndex =>
            tileIndexToUniqueIndex ?? (tileIndexToUniqueIndex = GetTileIndexToUniqueIndex());

        private int[][] blockedUniqueTileIndices;

        public int[][] BlockedUniqueTileIndices =>
            blockedUniqueTileIndices ?? (blockedUniqueTileIndices = GetBlockedUniqueTileIndices());

        private int[][] allowedUniqueTileIndices;

        public int[][] AllowedUniqueTileIndices =>
            allowedUniqueTileIndices ?? (allowedUniqueTileIndices = GetAllowedUniqueTileIndices());

        private int[,][] tileCompatibilityMatrix;

        public int[,][] TileCompatibilityMatrix => tileCompatibilityMatrix ??
                                                   (tileCompatibilityMatrix =
                                                       TileUtility.CreateTileCompatibilityMatrix(TileVariations, Grid));

        private bool[,,] tileCompatibilityLookup;

        public bool[,,] TileCompatibilityLookup => tileCompatibilityLookup ??
                                                   (tileCompatibilityLookup =
                                                       TileUtility.CreateTileCompatibilityLookup(TileVariations, Grid));


        public TileComposerInput([NotNull] Random random, [NotNull] IGrid grid,
            [NotNull] TileVariation[] tileVariations, [NotNull] TileVariation[][] blockedTiles)
        {
            Assert.IsNotNull(random);
            Assert.IsNotNull(grid);
            Assert.IsNotNull(tileVariations);
            Assert.IsNotNull(blockedTiles);

            Random = random;
            Grid = grid;
            TileVariations = tileVariations;
            BlockedTiles = blockedTiles;
        }

        private Dictionary<TileVariation, int> GetTileIndices()
        {
            return TileVariations
                .Select((s, i) => (s, i))
                .ToDictionary(x => x.s, x => x.i);
        }

        private TileVariation[][] FindUniqueVariations()
        {
            return (from variation in TileVariations
                    where variation.Tile != null
                    let uniqueParent = GetUniqueParentRecursive(variation.Tile)
                    group variation by (uniqueParent, variation.Rotation))
                .Select(x => x.ToArray())
                .Concat(TileVariations.Where(x => x.Tile == null).Select(x => new[] {x})) // Add empties
                .ToArray();
        }

        private static Tile GetUniqueParentRecursive(Tile tile)
        {
            if (IsUnique(tile))
            {
                return tile;
            }

            return GetUniqueParentRecursive(tile.BaseTile);
        }

        private static bool IsUnique(Tile tile)
        {
            return tile.BaseTile == null ||
                   tile.Neighbors.Any(x => x.Overrides.Any(y => y));
        }

        private int[] GetUniqueVariationRepresentatives()
        {
            var variationRepresentative = new int[UniqueVariations.Length];
            
            for (var i = 0; i < variationRepresentative.Length; i++)
            {
                variationRepresentative[i] = TileIndices[UniqueVariations[i][0]];
            }
            
            return variationRepresentative;
        }

        private int[] GetTileIndexToUniqueIndex()
        {
            var lookup = new int[TileVariations.Length];
            int variationIndex = 0;
            foreach (var variationGroup in UniqueVariations)
            {
                foreach (var tileVariation in variationGroup)
                {
                    lookup[TileIndices[tileVariation]] = variationIndex;
                }

                variationIndex++;
            }

            return lookup;
        }

        private int[][] GetBlockedUniqueTileIndices()
        {
            return BlockedTiles
                .Select(x => x == null ? new int[0] : x.Select(y => TileIndexToUniqueIndex[tileIndices[y]]).ToArray())
                .ToArray();
        }

        private int[][] GetAllowedUniqueTileIndices()
        {
            var variationCount = UniqueVariations.Length;

            var allowedUniqueTileIndices = new int[Grid.GridSize][];

            for (var i = 0; i < allowedUniqueTileIndices.Length; i++)
            {
                var hashSet = new HashSet<int>(BlockedUniqueTileIndices[i]);

                allowedUniqueTileIndices[i] =
                    Enumerable.Range(0, variationCount).Where(x => !hashSet.Contains(x)).ToArray();
            }

            return allowedUniqueTileIndices;
        }
    }
}