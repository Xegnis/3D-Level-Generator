using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using StixGames.TileComposer.Solvers.SATSolvers;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace StixGames.TileComposer.Solvers
{
    public class GeneralSATSolver : IConstraintSolver
    {
        public int ParallelInstances { get; set; }
        public bool SupportsSlowGeneration => false;
        public float Timeout { get; set; }
        public SolverResult CalculateModel() => Solve();

        public Task<SolverResult> CalculateModelAsync() => Task.Run(Solve);

        public SolverResult Step() => throw new NotImplementedException();

        public TileVariation[][] GetModel() => model;
        private readonly TileComposerInput input;
        private readonly SATSettings settings;
        private readonly ISATSolver solver;

        private int[][] tileVariables;
        private int[] fixedTiles;
        private TileVariation[][] model;

        private int lastTileVariable;

        private List<(int, int)> variableToTile;

        public GeneralSATSolver(TileComposerInput input, SATSettings settings, ISATSolver solver)
        {
            this.input = input;
            this.settings = settings;
            this.solver = solver;
        }

        private SolverResult Solve()
        {
            try
            {
                CreateAssertions();
            }
            catch (SATException e)
            {
                Debug.LogError(e.Message);

                return SolverResult.GuaranteedFailure;
            }

            var result = solver.Solve(lastTileVariable, Mathf.CeilToInt(Timeout));

            if (result == SolverResult.Success)
            {
                for (var i = 0; i < model.Length; i++)
                {
                    if (fixedTiles[i] >= 0)
                    {
                        model[i] = new[] {SelectTile(fixedTiles[i])};
                    }
                }

                var selectedTiles = solver.SelectedVariables;
                foreach (var selectedTile in selectedTiles)
                {
                    var (index, tileIndex) = variableToTile[selectedTile];

                    model[index] = new[] {SelectTile(tileIndex)};
                }
            }

            return result;
        }

        private void CreateAssertions()
        {
            // Collect all necessary variables
            fixedTiles = new int[input.Grid.GridSize];
            for (var i = 0; i < fixedTiles.Length; i++)
            {
                fixedTiles[i] = -1;
            }

            tileVariables = new int[input.Grid.GridSize][];

            // Create a lookup table for all variables. They start at 1, so add a dummy.
            variableToTile = new List<(int, int)> {(-1, -1)};

            model = new TileVariation[input.Grid.GridSize][];

            var order = Enumerable.Range(0, tileVariables.Length);

            if (settings.RandomizeConstraintOrder)
            {
                order = order.OrderBy(x => input.Random.Next());
            }

            foreach (var i in order)
            {
                // Fill the tile with 0 where no variables are necessary and a variable index for all other tile types
                var tile = new int[input.UniqueVariations.Length];

                var allowedVariables = input.AllowedUniqueTileIndices[i];

                if (allowedVariables.Length == 0)
                {
                    throw new SATException($"index {i} has no valid tiles");
                }
                else if (allowedVariables.Length == 1)
                {
                    // There is exactly one allowed type, just set that in the model and carry on
                    fixedTiles[i] = allowedVariables[0];
                }
                else
                {
                    // Create variables for each allowed type
                    foreach (var allowed in allowedVariables)
                    {
                        var nextVariable = solver.NextVariable();
                        tile[allowed] = nextVariable;
                        variableToTile.Add((i, allowed));
                    }
                }

                tileVariables[i] = tile;
            }

            lastTileVariable = solver.VariableCount;

            // Assert that there is at most one selection per tile
            for (var i = 0; i < tileVariables.Length; i++)
            {
                var tile = tileVariables[i];

                // Don't assert anything if the tile is already fixed
                if (fixedTiles[i] >= 0)
                {
                    continue;
                }

                // Assert that at most one variable is true
                var variables = tile.Where(x => x > 0).ToList();
                AssertExactlyOneCommanders(variables);
            }

            // Get all unique sides, remove opposites
            var sides = new List<int>();
            foreach (var side in Enumerable.Range(0, input.Grid.Sides))
            {
                if (!sides.Contains(input.Grid.GetNeighborSide(side)))
                {
                    sides.Add(side);
                }
            }

            // Neighbor restrictions
            for (var currentIndex = 0; currentIndex < tileVariables.Length; currentIndex++)
            {
                var allowedTiles = input.AllowedUniqueTileIndices[currentIndex];

                foreach (var side in sides)
                {
                    var otherIndex = input.Grid.GetNeighbor(currentIndex, side);
                    if (otherIndex < 0)
                    {
                        continue;
                    }

                    // Iterate all possible tiles
                    var otherAllowedTiles = input.AllowedUniqueTileIndices[otherIndex];
                    foreach (var allowedTile in allowedTiles)
                    {
                        var currentVariation = input.UniqueVariationRepresentatives[allowedTile];

                        foreach (var otherAllowedTile in otherAllowedTiles)
                        {
                            var otherVariation = input.UniqueVariationRepresentatives[otherAllowedTile];

                            // If currentVariation does not allow otherVariation as neighbor, block it.
                            if (!input.TileCompatibilityLookup[currentVariation, side, otherVariation])
                            {
                                AssertBlockNeighbor(currentIndex, allowedTile, otherIndex, otherAllowedTile);
                            }
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertBlockNeighbor(int currentIndex, int allowedTile, int otherIndex, int otherAllowedTile)
        {
            var current = tileVariables[currentIndex][allowedTile];
            var other = tileVariables[otherIndex][otherAllowedTile];

            if (current == 0 && other == 0)
            {
                // Both are fixed, but they are not legal neighbors
                throw new SATException($"Illegal fixed neighbors: {currentIndex} and {otherIndex}");
            }
            else if (current == 0)
            {
                // The other tile type is blocked
                solver.Assert(-other);
            }
            else if (other == 0)
            {
                // The current type is blocked
                solver.Assert(-current);
            }
            else
            {
                // One of the two has to be false
                solver.Assert(-current, -other);
            }
        }

        private void AssertExactlyOneCommanders(List<int> variables, int groupSize = 3)
        {
            var commanders = new List<int>();
            var naiveEncodingParam = new int[groupSize + 1];
            while (variables.Count > groupSize)
            {
                for (var i = 0; i < variables.Count; i += groupSize)
                {
                    // Single variables are treated as commander
                    if (i + 1 >= variables.Count)
                    {
                        commanders.Add(variables[i]);
                        continue;
                    }

                    // Create a commander for the current group
                    var commander = solver.NextVariable();
                    commanders.Add(commander);

                    // Copy the current group to the param array
                    int j;
                    for (j = 0; j < groupSize && j + i < variables.Count; j++)
                    {
                        naiveEncodingParam[j] = variables[j + i];
                    }

                    // Assert that either exactly one of the variables is true and the commander is true,
                    // or all variables are false and the commander is false
                    naiveEncodingParam[j++] = -commander;
                    AssertExactlyOneNaive(naiveEncodingParam, j);
                }

                // Swap arrays and clear commanders, to prevent allocations
                var temp = variables;
                variables = commanders;
                commanders = temp;
                commanders.Clear();
            }

            // Assert that exactly one of the last group of commanders is valid
            AssertExactlyOneNaive(variables, variables.Count);
        }

        private void AssertExactlyOneNaive(IReadOnlyList<int> variables, int size)
        {
            // Assert at least one
            solver.Assert(variables.Take(size));

            // Assert at most one
            for (int i = 0; i < size - 1; i++)
            {
                for (int j = i + 1; j < size; j++)
                {
                    solver.Assert(-variables[i], -variables[j]);
                }
            }
        }

        private TileVariation SelectTile(int uniqueIndex)
        {
            var variations = input.UniqueVariations[uniqueIndex];

            return variations[input.Random.Next(0, variations.Length)];
        }
        
        public void Reset()
        {
            solver.Reset();
        }

        private class SATException : Exception
        {
            public SATException(string message) : base(message)
            {
            }
        }
    }
}